using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

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

        /// <summary>
        /// Generate node using document and target name - proxies to model /upload
        /// </summary>
        Task<GenerateNodeResponseDto> GenerateNodeAsync(string targetPerson, string filename, string htmlContent);

        /// <summary>
        /// GraphRAG chat - proxies to model /chat
        /// </summary>
        Task<GraphRagChatResponseDto> GraphRagChatAsync(GraphRagChatRequestDto request);
    }
}

