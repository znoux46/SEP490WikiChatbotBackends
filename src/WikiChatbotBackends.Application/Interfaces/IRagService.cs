using System.Collections.Generic;
using System.IO;
using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IRagService
{
    /// <summary>
    /// Delete a document from the RAG system
    /// </summary>
    /// <param name="documentId">ID of the document to delete</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteDocumentAsync(string documentId);

    /// <summary>
    /// Chat with RAG system
    /// </summary>
    Task<ChatResponse> ChatAsync(ChatRequest request);

    /// <summary>
    /// Search in RAG system
    /// </summary>
    Task<SearchResponse> SearchAsync(SearchRequest request);

    /// <summary>
    /// Get the status of a processing job
    /// </summary>
    /// <param name="jobId">ID of the job</param>
    /// <returns>Job status information</returns>
    Task<JobStatusResponse> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Check if the RAG service is healthy and available
    /// </summary>
    /// <returns>True if service is healthy</returns>
    Task<bool> HealthCheckAsync();

    /// <summary>
    /// Upload document to RAG service
    /// </summary>
    Task<DocumentUploadResponse> UploadDocumentAsync(Stream fileStream, string fileName, int chunkSize = 800, int chunkOverlap = 150);

    /// <summary>
    /// Get list of documents
    /// </summary>
    Task<List<DocumentInfo>> GetDocumentsAsync(int skip = 0, int limit = 100);

    /// <summary>
    /// Get document by ID
    /// </summary>
    Task<DocumentInfo> GetDocumentByIdAsync(string documentId);

    /// <summary>
    /// Generate node using document and target name - proxies to model /upload
    /// </summary>
    Task<GenerateNodeResponseDto> GenerateNodeAsync(string targetPerson, string filename, string htmlContent);

    /// <summary>
    /// GraphRAG chat - proxies to model /chat
    /// </summary>
    Task<GraphRagChatResponseDto> GraphRagChatAsync(GraphRagChatRequestDto request);
}
