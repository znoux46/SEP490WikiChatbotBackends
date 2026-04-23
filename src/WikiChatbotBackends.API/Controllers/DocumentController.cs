using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Claims;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IWikipediaHtmlService _wikipediaHtmlService;
    private readonly IWikipediaService _wikipediaService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(
        IDocumentService documentService,
        IDocumentRepository documentRepository,
        IWikipediaHtmlService wikipediaHtmlService,
        IWikipediaService wikipediaService,
        IConfiguration configuration,
        ILogger<DocumentController> logger)
    {
        _documentService = documentService;
        _documentRepository = documentRepository;
        _wikipediaHtmlService = wikipediaHtmlService;
        _wikipediaService = wikipediaService;
        _configuration = configuration;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("pipeline-webhook")]
    [ProducesResponseType(typeof(DocumentPipelineWebhookResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentPipelineWebhookResultDto>> UpdatePipelineStatus(
        [FromBody] DocumentPipelineWebhookDto dto,
        CancellationToken cancellationToken)
    {
        var expectedToken = (_configuration["PipelineWebhook:Token"] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(expectedToken))
        {
            var incomingToken = Request.Headers["X-Webhook-Token"].ToString();
            if (!string.Equals(incomingToken, expectedToken, StringComparison.Ordinal))
            {
                _logger.LogWarning("Pipeline webhook rejected due to invalid token");
                return Unauthorized(new { message = "Invalid webhook token" });
            }
        }

        try
        {
            var result = await _documentService.UpdatePipelineStatusAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("update-from-wikipedia")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateFromWikipedia([FromBody] WikipediaUpdateDto dto, CancellationToken cancellationToken)
    {
        try
        {
            if (dto.CategoryId == Guid.Empty)
                return BadRequest(new { message = "CategoryId is required" });

            if (string.IsNullOrEmpty(dto.PageTitle))
                return BadRequest(new { message = "PageTitle is required" });

            var language = string.IsNullOrEmpty(dto.Language) ? "vi" : dto.Language;

            // Normalize page title (handle URLs or other formats)
            var pageTitle = dto.PageTitle.Trim();

            _logger.LogInformation("Fetching Wikipedia page: {PageTitle} (language: {Language})", pageTitle, language);

            var wikipediaResult = await _wikipediaHtmlService.FetchProcessedWikipediaHtmlAsync(pageTitle, language);
            var wikipediaSummary = await _wikipediaService.GetArticleSummaryAsync(pageTitle, language);
            var contentPreview = BuildTwoLineContent(wikipediaSummary);

            var existingDocument = await _documentRepository.GetByCategoryAndWikipediaUrlAsync(dto.CategoryId, wikipediaResult.WikipediaUrl);
            var jobIds = new List<string>();
            Guid resultDocumentId = Guid.Empty;

            if (existingDocument != null)
            {
                // REUSE existing document instead of delete+create (prevents duplicates)
                _logger.LogInformation(
                    "Reusing existing Wikipedia document {DocumentId} for category {CategoryId} with URL {WikipediaUrl}",
                    existingDocument.Id,
                    dto.CategoryId,
                    wikipediaResult.WikipediaUrl
                );

                existingDocument.Content = wikipediaResult.HtmlContent;
                existingDocument.Description = contentPreview;
                existingDocument.ThumbnailUrl = wikipediaResult.ThumbnailUrl;
                existingDocument.UpdatedAt = DateTime.UtcNow;
                await _documentRepository.UpdateAsync(existingDocument);

                resultDocumentId = existingDocument.Id;

                var taskId = ExtractTaskIdFromMetadata(existingDocument.Metadata);
                if (!string.IsNullOrWhiteSpace(taskId))
                {
                    jobIds.Add(taskId);
                }
            }
            else
            {
                // CREATE new document only if doesn't exist
                var safeFileName = SanitizeFileName(dto.PageTitle);
                var fileName = $"{safeFileName}.html";
                var htmlBytes = Encoding.UTF8.GetBytes(wikipediaResult.HtmlContent);
                var contentType = "text/html";

                var inputFiles = new List<DocumentUploadItemDto>
                {
                    new DocumentUploadItemDto
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        Length = htmlBytes.LongLength,
                        ThumbnailUrl = wikipediaResult.ThumbnailUrl,
                        OpenReadStream = () => new MemoryStream(htmlBytes, writable: false)
                    }
                };

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue("userId")
                             ?? "anonymous";

                var uploadedDocuments = await _documentService.UploadAsync(dto.CategoryId, userId, inputFiles, cancellationToken);

                // Persist canonical Wikipedia URL on created documents
                foreach (var uploadedDocument in uploadedDocuments)
                {
                    var document = await _documentRepository.GetByIdAsync(uploadedDocument.Id);
                    if (document == null)
                    {
                        continue;
                    }

                    document.WikipediaUrl = wikipediaResult.WikipediaUrl;
                    document.ThumbnailUrl = wikipediaResult.ThumbnailUrl;
                    document.Content = wikipediaResult.HtmlContent;
                    document.Description = contentPreview;
                    document.UpdatedAt = DateTime.UtcNow;
                    await _documentRepository.UpdateAsync(document);

                    resultDocumentId = document.Id;

                    var taskId = ExtractTaskIdFromMetadata(document.Metadata);
                    if (!string.IsNullOrWhiteSpace(taskId))
                    {
                        jobIds.Add(taskId);
                    }
                }
            }

            var uniqueJobIds = jobIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return Ok(new
            {
                message = "Wikipedia content fetched and uploaded successfully",
                categoryId = dto.CategoryId,
                pageTitle = dto.PageTitle,
                language = language,
                wikipediaUrl = wikipediaResult.WikipediaUrl,
                thumbnailUrl = wikipediaResult.ThumbnailUrl,
                documentId = resultDocumentId != Guid.Empty ? resultDocumentId.ToString() : (string?)null,
                jobId = uniqueJobIds.FirstOrDefault() ?? string.Empty,
                jobIds = uniqueJobIds,
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating from Wikipedia: {PageTitle}", dto.PageTitle);
            return BadRequest(new { message = "An error occurred while fetching Wikipedia content", error = ex.Message });
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var sanitized = Regex.Replace(fileName, "[^\\w\\-. ]", "_");
        sanitized = sanitized.Replace(" ", "_").Trim('_', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "wikipedia-document" : sanitized;
    }

    private static string ExtractTaskIdFromMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return string.Empty;
        }

        try
        {
            using var json = JsonDocument.Parse(metadata);
            if (json.RootElement.TryGetProperty("task_id", out var taskIdElement)
                && taskIdElement.ValueKind == JsonValueKind.String)
            {
                return taskIdElement.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Ignore malformed metadata; endpoint still succeeds.
        }

        return string.Empty;
    }

    private static string BuildTwoLineContent(WikipediaSummaryResponse? summary)
    {
        if (summary == null)
        {
            return string.Empty;
        }

        var source = !string.IsNullOrWhiteSpace(summary.Extract)
            ? summary.Extract
            : (summary.Description ?? string.Empty);

        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var normalized = source.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        var lines = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .ToList();

        if (lines.Count == 2)
        {
            return string.Join("\n", lines);
        }

        var sentences = Regex.Split(normalized, @"(?<=[.!?])\s+")
            .Select(static sentence => sentence.Trim())
            .Where(static sentence => !string.IsNullOrWhiteSpace(sentence))
            .Take(2)
            .ToList();

        if (sentences.Count > 0)
        {
            return string.Join("\n", sentences);
        }

        return lines.Count > 0 ? lines[0] : string.Empty;
    }

    [HttpGet("search-from-wikipedia")]
    [ProducesResponseType(typeof(List<WikipediaDocumentSearchItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<WikipediaDocumentSearchItemDto>>> SearchFromWikipedia(
        [FromQuery] string keyword,
        [FromQuery] string language = "vi",
        [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Keyword is required" });

        if (limit < 1 || limit > 20)
            return BadRequest(new { message = "Limit must be between 1 and 20" });

        var result = await _wikipediaService.SearchDocumentsAsync(keyword, language, limit);
        return Ok(result);
    }
}
