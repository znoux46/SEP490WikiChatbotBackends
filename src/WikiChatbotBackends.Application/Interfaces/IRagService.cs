using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces
{
    /// <summary>
    /// Interface for RAG (Retrieval-Augmented Generation) service
    /// Communicates with the Python FastAPI RAG service
    /// </summary>
    public interface IRagService
    {
        /// <summary>
        /// Send a question to the RAG service and get an AI-generated answer
        /// </summary>
        /// <param name="request">Chat request containing the question</param>
        /// <returns>Chat response with the answer and metadata</returns>
        Task<ChatResponse> ChatAsync(ChatRequest request);

        /// <summary>
        /// Search for relevant document chunks
        /// </summary>
        /// <param name="request">Search request with query and parameters</param>
        /// <returns>Search response with matching chunks</returns>
        Task<SearchResponse> SearchAsync(SearchRequest request);

        /// <summary>
        /// Upload a document to the RAG service for processing
        /// </summary>
        /// <param name="fileStream">File stream to upload</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="chunkSize">Size of chunks (default: 800)</param>
        /// <param name="chunkOverlap">Overlap between chunks (default: 150)</param>
        /// <returns>Upload response with job information</returns>
        Task<DocumentUploadResponse> UploadDocumentAsync(
            Stream fileStream, 
            string fileName, 
            int chunkSize = 800, 
            int chunkOverlap = 150);

        /// <summary>
        /// Get list of all documents in the RAG system
        /// </summary>
        /// <param name="skip">Number of documents to skip (pagination)</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>List of document information</returns>
        Task<List<DocumentInfo>> GetDocumentsAsync(int skip = 0, int limit = 100);

        /// <summary>
        /// Get detailed information about a specific document
        /// </summary>
        /// <param name="documentId">ID of the document</param>
        /// <returns>Document information</returns>
        Task<DocumentInfo> GetDocumentByIdAsync(string documentId);

        /// <summary>
        /// Delete a document from the RAG system
        /// </summary>
        /// <param name="documentId">ID of the document to delete</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteDocumentAsync(string documentId);

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
    }
}
