using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.Infrastructure.Services
{
    public class RagService : IRagService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RagService> _logger;
        private readonly string _baseUrl;

        public RagService(HttpClient httpClient, IConfiguration configuration, ILogger<RagService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["RagService:BaseUrl"] ?? "http://localhost:8000";
            
            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(
                int.Parse(configuration["RagService:Timeout"] ?? "120")
            );
        }

        public async Task<ChatResponse> ChatAsync(ChatRequest request)
        {
            try
            {
                _logger.LogInformation("Sending chat request to RAG service: {Question}", request.Question);

                var response = await _httpClient.PostAsJsonAsync("/chat", new
                {
                    question = request.Question,
                    verbose = request.Verbose
                });

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                
                if (result == null)
                {
                    throw new Exception("Failed to deserialize chat response");
                }

                _logger.LogInformation("Received chat response from RAG service");
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while calling RAG chat endpoint");
                throw new Exception($"Failed to communicate with RAG service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatAsync");
                throw;
            }
        }

        public async Task<SearchResponse> SearchAsync(SearchRequest request)
        {
            try
            {
                _logger.LogInformation("Sending search request to RAG service: {Query}", request.Query);

                var response = await _httpClient.PostAsJsonAsync("/api/v1/search", new
                {
                    query = request.Query,
                    top_k = request.TopK,
                    document_ids = request.DocumentIds,
                    search_type = request.SearchType,
                    bm25_weight = request.Bm25Weight,
                    semantic_weight = request.SemanticWeight
                });

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
                
                if (result == null)
                {
                    throw new Exception("Failed to deserialize search response");
                }

                _logger.LogInformation("Received {Count} search results from RAG service", result.Total);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while calling RAG search endpoint");
                throw new Exception($"Failed to communicate with RAG service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchAsync");
                throw;
            }
        }

        public async Task<DocumentUploadResponse> UploadDocumentAsync(
            Stream fileStream, 
            string fileName, 
            int chunkSize = 800, 
            int chunkOverlap = 150)
        {
            try
            {
                _logger.LogInformation("Uploading document to RAG service: {FileName}", fileName);

                using var content = new MultipartFormDataContent();
                
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "files", fileName);
                content.Add(new StringContent(chunkSize.ToString()), "chunk_size");
                content.Add(new StringContent(chunkOverlap.ToString()), "chunk_overlap");

                var response = await _httpClient.PostAsync("/api/v1/process", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<DocumentUploadResponse>();
                
                if (result == null)
                {
                    throw new Exception("Failed to deserialize upload response");
                }

                _logger.LogInformation("Document uploaded successfully: {FileName}", fileName);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while uploading document");
                throw new Exception($"Failed to upload document to RAG service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadDocumentAsync");
                throw;
            }
        }

        public async Task<List<DocumentInfo>> GetDocumentsAsync(int skip = 0, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Fetching documents from RAG service (skip: {Skip}, limit: {Limit})", skip, limit);

                var response = await _httpClient.GetAsync($"/api/v1/documents?skip={skip}&limit={limit}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<List<DocumentInfo>>();
                
                if (result == null)
                {
                    throw new Exception("Failed to deserialize documents response");
                }

                _logger.LogInformation("Fetched {Count} documents from RAG service", result.Count);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching documents");
                throw new Exception($"Failed to fetch documents from RAG service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDocumentsAsync");
                throw;
            }
        }

        public async Task<DocumentInfo> GetDocumentByIdAsync(string documentId)
        {
            try
            {
                _logger.LogInformation("Fetching document {DocumentId} from RAG service", documentId);

                var response = await _httpClient.GetAsync($"/api/v1/documents/{documentId}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<DocumentInfo>();
                
                if (result == null)
                {
                    throw new Exception("Failed to deserialize document response");
                }

                _logger.LogInformation("Fetched document {DocumentId} from RAG service", documentId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching document {DocumentId}", documentId);
                throw new Exception($"Failed to fetch document from RAG service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDocumentByIdAsync");
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string documentId)
        {
            try
            {
                _logger.LogInformation("Deleting document {DocumentId} from RAG service", documentId);

                var response = await _httpClient.DeleteAsync($"/api/v1/documents/{documentId}");
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Deleted document {DocumentId} from RAG service", documentId);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while deleting document {DocumentId}", documentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteDocumentAsync");
                return false;
            }
        }

        public async Task<JobStatusResponse> GetJobStatusAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Fetching job status {JobId} from RAG service", jobId);

                var response = await _httpClient.GetAsync($"/api/v1/status/{jobId}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
                
                if (result == null)
                {
                    throw new Exception("Failed to deserialize job status response");
                }

                _logger.LogInformation("Fetched job status {JobId}: {Status}", jobId, result.Status);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching job status {JobId}", jobId);
                throw new Exception($"Failed to fetch job status from RAG service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobStatusAsync");
                throw;
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                _logger.LogInformation("Checking RAG service health");

                var response = await _httpClient.GetAsync("/health");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("RAG service is healthy");
                    return true;
                }

                _logger.LogWarning("RAG service health check failed with status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RAG service health check");
                return false;
            }
        }
    }
}
