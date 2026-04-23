using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Services;

public class DocumentService : IDocumentService
{
    private const int MaxSingleFileSizeBytes = 10 * 1024 * 1024;
    private const int MaxBatchFiles = 5;

    private readonly IDocumentRepository _documentRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IDocumentStorageService _documentStorageService;
    private readonly IDocumentQueueService? _documentQueueService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        ICategoryRepository categoryRepository,
        IDocumentStorageService documentStorageService,
        IDocumentQueueService? documentQueueService,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _categoryRepository = categoryRepository;
        _documentStorageService = documentStorageService;
        _documentQueueService = documentQueueService;
        _logger = logger;
    }

    public async Task<List<DocumentDto>> GetAllAsync(Guid? categoryId = null)
    {
        var docs = categoryId.HasValue
            ? await _documentRepository.GetByCategoryIdWithCategoryAsync(categoryId.Value)
            : await _documentRepository.GetAllWithCategoryAsync();

        return docs.Select(MapToDto).ToList();
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id)
    {
        var document = await _documentRepository.GetByIdWithCategoryAsync(id);
        return document == null ? null : MapToDto(document);
    }

    public async Task<List<DocumentDto>> UploadAsync(Guid categoryId, string userId, List<DocumentUploadItemDto> files, CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
            throw new ArgumentException("No file uploaded");

        if (files.Count > MaxBatchFiles)
            throw new ArgumentException($"Batch upload supports at most {MaxBatchFiles} files");

        if (!await _categoryRepository.ExistsByIdAsync(categoryId))
            throw new KeyNotFoundException($"Category {categoryId} not found");

        foreach (var file in files)
        {
            if (file.Length <= 0)
                throw new ArgumentException($"File {file.FileName} is empty");

            if (file.Length > MaxSingleFileSizeBytes)
                throw new ArgumentException($"File {file.FileName} exceeds 10MB limit");
        }

        _logger.LogInformation("Uploading {Count} document(s) for category {CategoryId}", files.Count, categoryId);

        // Upload all files to Cloudinary first, then persist successful upload records to DB.
        var uploadTasks = files.Select(file => _documentStorageService.UploadAsync(file, cancellationToken)).ToList();
        var uploadResults = await Task.WhenAll(uploadTasks);

        var created = new List<Document>();
        var enqueueTasks = new List<Task>();

        for (var i = 0; i < files.Count; i++)
        {
            var result = uploadResults[i];
            var file = files[i];
            var jobId = Guid.NewGuid().ToString();

            var metadataJson = JsonSerializer.Serialize(new
            {
                publicId = result.PublicId,
                resourceType = result.ResourceType,
                format = result.Format,
                bytes = result.Bytes,
                task_id = jobId,
                pipeline_status = new
                {
                    rag = "pending",
                    graph = "pending",
                    updated_at = DateTime.UtcNow
                }
            });

            var entity = new Document
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                FileName = file.FileName,
                FilePath = result.Url,
                SourceType = "cloudinary",
                Status = "pending",
                FileSize = (int)Math.Min(file.Length, int.MaxValue),
                Metadata = metadataJson,
                ThumbnailUrl = file.ThumbnailUrl,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _documentRepository.AddAsync(entity);
            created.Add(entity);

            if (_documentQueueService != null)
            {
                var fileType = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
                var job = new DocumentProcessingJobDto
                {
                    JobId = jobId,
                    DocumentId = entity.Id.ToString(),
                    UserId = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId,
                    FileName = file.FileName,
                    FileUrl = result.Url,
                    FileType = string.IsNullOrWhiteSpace(fileType) ? "unknown" : fileType,
                    RetryCount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                enqueueTasks.Add(_documentQueueService.EnqueueTaskAsync(job, cancellationToken));
            }
        }

        // Enqueue all jobs in parallel
        if (enqueueTasks.Count > 0)
        {
            await Task.WhenAll(enqueueTasks);
        }

        var createdIds = created.Select(d => d.Id).ToHashSet();
        var docsWithCategory = await _documentRepository.GetAllWithCategoryAsync();

        return docsWithCategory
            .Where(d => createdIds.Contains(d.Id))
            .Select(MapToDto)
            .ToList();
    }

    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto)
    {
        var document = await _documentRepository.GetByIdWithCategoryAsync(id);
        if (document == null)
            throw new KeyNotFoundException($"Document {id} not found");

        if (string.IsNullOrWhiteSpace(dto.FileName))
            throw new ArgumentException("FileName is required");

        document.FileName = dto.FileName;
        document.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(document);
        return MapToDto(document);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            throw new KeyNotFoundException($"Document {id} not found");

        var (publicId, resourceType, _, _) = ExtractCloudinaryInfo(document.Metadata);
        if (!string.IsNullOrWhiteSpace(publicId))
            await _documentStorageService.DeleteAsync(publicId, resourceType, cancellationToken);

        document.IsDeleted = true;
        document.Status = "deleted";
        document.UpdatedAt = DateTime.UtcNow;
        await _documentRepository.UpdateAsync(document);
    }

    public async Task<DocumentPipelineWebhookResultDto> UpdatePipelineStatusAsync(DocumentPipelineWebhookDto dto, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (dto == null)
            throw new ArgumentException("Payload is required");

        var pipeline = NormalizePipeline(dto.Pipeline);
        var incomingStatus = NormalizePipelineStatus(dto.Status);

        if (pipeline == "unknown")
            throw new ArgumentException("Pipeline must be 'rag' or 'graph'");

        if (incomingStatus == "unknown")
            throw new ArgumentException("Status must be one of: pending, processing, completed, failed");

        Document? document = null;
        if (dto.DocumentId.HasValue && dto.DocumentId.Value != Guid.Empty)
        {
            document = await _documentRepository.GetByIdAsync(dto.DocumentId.Value);
        }

        if (document == null && !string.IsNullOrWhiteSpace(dto.TaskId))
        {
            document = await _documentRepository.GetByTaskIdAsync(dto.TaskId!);
        }

        if (document == null)
            throw new KeyNotFoundException("Document not found by documentId/taskId");

        var metadata = EnsureMetadataObject(document.Metadata);
        var pipelineStatus = metadata["pipeline_status"] as JsonObject ?? new JsonObject();

        var ragStatus = NormalizePipelineStatus(pipelineStatus["rag"]?.GetValue<string>());
        var graphStatus = NormalizePipelineStatus(pipelineStatus["graph"]?.GetValue<string>());

        if (pipeline == "rag")
            ragStatus = incomingStatus;
        else
            graphStatus = incomingStatus;

        pipelineStatus["rag"] = ragStatus;
        pipelineStatus["graph"] = graphStatus;
        pipelineStatus["updated_at"] = DateTime.UtcNow.ToString("O");

        metadata["pipeline_status"] = pipelineStatus;

        if (!string.IsNullOrWhiteSpace(dto.TaskId))
            metadata["task_id"] = dto.TaskId;

        var pipelineMessages = metadata["pipeline_messages"] as JsonObject ?? new JsonObject();
        if (!string.IsNullOrWhiteSpace(dto.Message))
            pipelineMessages[pipeline] = dto.Message;
        if (!string.IsNullOrWhiteSpace(dto.Error))
            pipelineMessages[$"{pipeline}_error"] = dto.Error;
        if (pipelineMessages.Count > 0)
            metadata["pipeline_messages"] = pipelineMessages;

        if (!string.IsNullOrWhiteSpace(dto.FileName) && string.IsNullOrWhiteSpace(document.FileName))
            document.FileName = dto.FileName;

        document.Status = ComposeDocumentStatus(ragStatus, graphStatus);
        document.Metadata = metadata.ToJsonString();
        document.UpdatedAt = DateTime.UtcNow;

        await _documentRepository.UpdateAsync(document);

        _logger.LogInformation(
            "Pipeline webhook updated document {DocumentId}: rag={RagStatus}, graph={GraphStatus}, status={ComposedStatus}",
            document.Id,
            ragStatus,
            graphStatus,
            document.Status
        );

        return new DocumentPipelineWebhookResultDto
        {
            DocumentId = document.Id,
            Status = document.Status ?? "pending",
            RagStatus = ragStatus,
            GraphStatus = graphStatus,
            TaskId = metadata["task_id"]?.GetValue<string>()
        };
    }

    private static DocumentDto MapToDto(Document document)
    {
        var (publicId, resourceType, format, bytes) = ExtractCloudinaryInfo(document.Metadata);

        return new DocumentDto
        {
            Id = document.Id,
            CategoryId = document.CategoryId,
            CategoryName = document.Category?.Name ?? string.Empty,
            FileName = document.FileName,
            Content = document.Content,
            Status = document.Status ?? string.Empty,
            Url = document.FilePath,
            PublicId = publicId,
            ResourceType = resourceType,
            Format = format,
            ThumbnailUrl = document.ThumbnailUrl,
            Bytes = bytes > 0 ? bytes : (document.FileSize ?? 0),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }

    private static (string publicId, string resourceType, string? format, long bytes) ExtractCloudinaryInfo(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return (string.Empty, "raw", null, 0);

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            var root = doc.RootElement;

            var publicId = root.TryGetProperty("publicId", out var p) ? p.GetString() ?? string.Empty : string.Empty;
            var resourceType = root.TryGetProperty("resourceType", out var r) ? r.GetString() ?? "raw" : "raw";
            var format = root.TryGetProperty("format", out var f) ? f.GetString() : null;
            var bytes = root.TryGetProperty("bytes", out var b) && b.TryGetInt64(out var parsedBytes) ? parsedBytes : 0;

            return (publicId, resourceType, format, bytes);
        }
        catch
        {
            return (string.Empty, "raw", null, 0);
        }
    }

    private static string NormalizePipeline(string? pipeline)
    {
        var normalized = (pipeline ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "rag" => "rag",
            "graph" => "graph",
            "graph-rag" => "graph",
            "graphrag" => "graph",
            _ => "unknown"
        };
    }

    private static string NormalizePipelineStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "pending" => "pending",
            "queued" => "pending",
            "processing" => "processing",
            "indexing" => "processing",
            "completed" => "completed",
            "complete" => "completed",
            "done" => "completed",
            "failed" => "failed",
            "error" => "failed",
            _ => "unknown"
        };
    }

    private static string ComposeDocumentStatus(string ragStatus, string graphStatus)
    {
        if (ragStatus == "failed" || graphStatus == "failed")
            return "failed";

        if (ragStatus == "completed" && graphStatus == "completed")
            return "completed";

        if (ragStatus == "completed")
            return "rag_completed_waiting_graph";

        if (ragStatus == "processing" || graphStatus == "processing")
            return "indexing";

        return "pending";
    }

    private static JsonObject EnsureMetadataObject(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return new JsonObject();

        try
        {
            var node = JsonNode.Parse(metadata) as JsonObject;
            return node ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }
}
