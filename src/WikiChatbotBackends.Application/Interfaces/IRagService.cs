using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IRagService
{
    Task<JobStatusResponse> ChatGraphRagAsync(GraphRagRequestDto request);

    Task<ChatResponse> ChatAsync(ChatRequest request);
    Task<SearchResponse> SearchAsync(SearchRequest request);
    Task<DocumentUploadResponse> UploadDocumentAsync(Stream fileStream, string fileName, int chunkSize = 800, int chunkOverlap = 150);
    Task<EditDocumentJobResponse> EditDocumentAsync(string documentId, Stream fileStream, string fileName, int chunkSize = 800, int chunkOverlap = 150);
    Task<List<DocumentInfo>> GetDocumentsAsync(int skip = 0, int limit = 100);
    Task<DocumentInfo> GetDocumentByIdAsync(string documentId);
    Task<bool> DeleteDocumentAsync(string documentId);
    Task<JobStatusResponse> GetJobStatusAsync(string jobId);
    Task<bool> HealthCheckAsync();
    Task<GenerateNodeResponseDto> GenerateNodeAsync(string targetPerson, string filename, string htmlContent);
    Task<ChatResponse> GraphRagChatAsync(ChatRequest request);
    Task<GraphRagJobStatusResponseDTO?> GetNodeStatusAsync(string jobId);
}

