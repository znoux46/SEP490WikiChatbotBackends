using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IRagService
{
    // Existing methods...
    Task<JobStatusResponse> IngestNewAsync(GraphRagRequestDto request);
    Task<JobStatusResponse> MigrateNewAsync(GraphRagMigrateDto request);
    
    // Existing...
    Task<ChatResponse> ChatAsync(ChatRequest request);
    Task<SearchResponse> SearchAsync(SearchRequest request);
    Task<DocumentUploadResponse> UploadDocumentAsync(Stream fileStream, string fileName, int chunkSize = 800, int chunkOverlap = 150);
    Task<List<DocumentInfo>> GetDocumentsAsync(int skip = 0, int limit = 100);
    Task<DocumentInfo> GetDocumentByIdAsync(string documentId);
    Task<bool> DeleteDocumentAsync(string documentId);
    Task<JobStatusResponse> GetJobStatusAsync(string jobId);
    Task<bool> HealthCheckAsync();
}

