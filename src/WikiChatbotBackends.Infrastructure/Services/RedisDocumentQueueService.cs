using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.Infrastructure.Services;

public class RedisDocumentQueueService : IDocumentQueueService
{
    private const string DefaultSharedTaskQueue = "document:task:queue";
    private const string DefaultSharedProcessingQueue = "document:processing:queue";
    private const string DefaultSharedFailedQueue = "document:failed:queue";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDocumentQueueService> _logger;

    private readonly string _ragTaskQueue;
    private readonly string _ragProcessingQueue;
    private readonly string _ragFailedQueue;

    private readonly string _graphTaskQueue;
    private readonly string _graphProcessingQueue;
    private readonly string _graphFailedQueue;

    private sealed class PipelineSnapshot
    {
        public string Status { get; init; } = "NOT_FOUND";
        public object? Payload { get; init; }
        public string? Message { get; init; }
    }

    public RedisDocumentQueueService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisDocumentQueueService> logger)
    {
        _redis = redis;
        _logger = logger;

        // Shared queue config (single queue set for both RAG + Graph workers)
        _ragTaskQueue =
            configuration["Redis:Queues:TaskQueue"]
            ?? DefaultSharedTaskQueue;
        _ragProcessingQueue =
            configuration["Redis:Queues:ProcessingQueue"]
            ?? DefaultSharedProcessingQueue;
        _ragFailedQueue =
            configuration["Redis:Queues:FailedQueue"]
            ?? DefaultSharedFailedQueue;

        _graphTaskQueue = _ragTaskQueue;
        _graphProcessingQueue = _ragProcessingQueue;
        _graphFailedQueue = _ragFailedQueue;
    }

    // Sentinel value pushed to task queues on startup so the Redis key never
    // disappears when the queue is empty (Redis auto-deletes empty lists).
    // Include the queue name so both RAG and Graph workers can recognize the
    // marker consistently after restarts.
    private static string BuildQueueSentinelPayload(string queueName)
    {
        return JsonSerializer.Serialize(new
        {
            type = "sentinel",
            sentinel = true,
            queue = queueName
        });
    }

    public Task EnqueueTaskAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default)
    {
        return EnqueueToBothPipelinesAsync(job, _ragTaskQueue, _graphTaskQueue, cancellationToken);
    }

    public async Task EnqueueGraphTaskAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = _redis.GetDatabase();
        var payload = BuildPayload(job, "graph");
        await db.ListRightPushAsync(_graphTaskQueue, payload);
        _logger.LogInformation(
            "Enqueued document job {JobId} to Graph pipeline only: {GraphQueue}",
            job.JobId,
            _graphTaskQueue
        );
    }

    public async Task EnsureTaskQueuesInitializedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = _redis.GetDatabase();
        foreach (var queueName in new[] { _ragTaskQueue, _graphTaskQueue })
        {
            var exists = await db.KeyExistsAsync(queueName);
            if (!exists)
            {
                var sentinelPayload = BuildQueueSentinelPayload(queueName);
                // LPUSH puts the sentinel at the LEFT (head) so real tasks pushed
                // to the RIGHT (tail) are consumed first by BRPOPLPUSH.
                await db.ListLeftPushAsync(queueName, sentinelPayload);
                _logger.LogInformation("Initialized task queue with sentinel: {Queue}", queueName);
            }
        }
    }

    public Task EnqueueProcessingAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default)
    {
        return EnqueueToBothPipelinesAsync(job, _ragProcessingQueue, _graphProcessingQueue, cancellationToken);
    }

    public Task EnqueueFailedAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default)
    {
        return EnqueueToBothPipelinesAsync(job, _ragFailedQueue, _graphFailedQueue, cancellationToken);
    }

    private async Task EnqueueToBothPipelinesAsync(
        DocumentProcessingJobDto job,
        string ragQueueName,
        string graphQueueName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = _redis.GetDatabase();

        var ragPayload = BuildPayload(job, "rag");
        var graphPayload = BuildPayload(job, "graph");

        await Task.WhenAll(
            db.ListRightPushAsync(ragQueueName, ragPayload),
            db.ListRightPushAsync(graphQueueName, graphPayload)
        );

        _logger.LogInformation(
            "Enqueued document job {JobId} to both pipelines: RAG={RagQueue}, Graph={GraphQueue}",
            job.JobId,
            ragQueueName,
            graphQueueName
        );
    }

    private static string BuildPayload(DocumentProcessingJobDto job, string pipeline)
    {
        return JsonSerializer.Serialize(new
        {
            type = pipeline,
            job_id = job.JobId,
            document_id = job.DocumentId,
            user_id = job.UserId,
            file_name = job.FileName,
            file_url = job.FileUrl,
            file_type = job.FileType,
            retry_count = job.RetryCount,
            created_at = job.CreatedAt,
            pipeline
        });
    }

    public async Task<object> GetCombinedStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = _redis.GetDatabase();

        // Get status from both RAG and Graph pipelines
        var ragStatusKey = $"rag:task:status:{jobId}";
        var graphStatusKey = $"graphrag:task:status:{jobId}";

        var ragResultKey = $"rag:result:{jobId}";
        var graphResultKey = $"graphrag:result:{jobId}";

        var ragStatusValue = await db.StringGetAsync(ragStatusKey);
        var graphStatusValue = await db.StringGetAsync(graphStatusKey);
        var ragResult = await db.StringGetAsync(ragResultKey);
        var graphResult = await db.StringGetAsync(graphResultKey);

        var ragSnapshot = ParsePipelineSnapshot(ragStatusValue);
        var graphSnapshot = ParsePipelineSnapshot(graphStatusValue);

        var response = new
        {
            jobId,
            timestamp = DateTime.UtcNow,
            rag = new
            {
                status = ragSnapshot.Status,
                message = ragSnapshot.Message,
                payload = ragSnapshot.Payload,
                result = ragResult.IsNull ? null : ragResult.ToString(),
                statusKey = ragStatusKey
            },
            graph = new
            {
                status = graphSnapshot.Status,
                message = graphSnapshot.Message,
                payload = graphSnapshot.Payload,
                result = graphResult.IsNull ? null : graphResult.ToString(),
                statusKey = graphStatusKey
            }
        };

        _logger.LogInformation(
            "Retrieved combined status for job {JobId}: RAG={RagStatus} ({RagStatusKey}), Graph={GraphStatus} ({GraphStatusKey})",
            jobId,
            ragSnapshot.Status,
            ragStatusKey,
            graphSnapshot.Status,
            graphStatusKey
        );

        return response;
    }

    private static PipelineSnapshot ParsePipelineSnapshot(RedisValue rawStatus)
    {
        if (rawStatus.IsNullOrEmpty)
        {
            return new PipelineSnapshot();
        }

        var rawText = rawStatus.ToString();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new PipelineSnapshot();
        }

        try
        {
            using var doc = JsonDocument.Parse(rawText);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString() ?? "NOT_FOUND"
                : "NOT_FOUND";
            var message = root.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : null;

            // Clone to decouple from disposed JsonDocument while keeping full payload for diagnostics.
            var payload = root.Clone();

            return new PipelineSnapshot
            {
                Status = status,
                Message = message,
                Payload = payload,
            };
        }
        catch
        {
            // Backward compatibility for legacy plain-string status values.
            return new PipelineSnapshot
            {
                Status = rawText,
                Payload = rawText,
            };
        }
    }

    public async Task<int> PurgeOldQueuesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var server = _redis.GetServer(_redis.GetEndPoints().First());
        int keysDeleted = 0;

        // Old queue key names to delete
        var oldQueueKeys = new[]
        {
            "document:task:queue",
            "document:processing:queue",
            "document:failed:queue",
            "document:dead-letter:queue"
        };

        foreach (var key in oldQueueKeys)
        {
            var deleted = await server.ExecuteAsync("DEL", key);
            if (deleted.Type == ResultType.Integer)
            {
                int count = (int)deleted;
                keysDeleted += count;
                _logger.LogInformation("Deleted old queue key: {Key} ({Count} elements)", key, count);
            }
        }

        _logger.LogWarning("Purged old queue keys. Total elements deleted: {Count}", keysDeleted);

        return keysDeleted;
    }
}

