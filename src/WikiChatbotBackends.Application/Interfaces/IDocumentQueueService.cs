using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDocumentQueueService
{
    Task EnqueueTaskAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default);
    Task EnqueueGraphTaskAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default);
    Task EnqueueProcessingAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default);
    Task EnqueueFailedAsync(DocumentProcessingJobDto job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get combined status for both RAG and Graph pipelines for a given job ID
    /// </summary>
    Task<object> GetCombinedStatusAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purge old queue keys (document:task:queue, document:processing:queue, document:failed:queue)
    /// This is safe to run in development to clean up old jobs
    /// </summary>
    Task<int> PurgeOldQueuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure all task queue keys exist in Redis by pushing a sentinel value if absent.
    /// This prevents queue keys from disappearing when empty (Redis deletes empty lists).
    /// </summary>
    Task EnsureTaskQueuesInitializedAsync(CancellationToken cancellationToken = default);
}
