using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WikiChatbotBackends.Infrastructure.Services
{
    public class RagService : IRagService
    {
        public async Task<JobStatusResponse> ChatGraphRagAsync(GraphRagRequestDto request)
        {
            try
            {
                _logger.LogInformation("Chat GraphRAG: {Question}", request.Question);
                var response = await _httpClient.PostAsJsonAsync("/chat", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JobStatusResponse>() ?? throw new Exception("Invalid response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat GraphRAG failed");
                throw;
            }
        }

        private readonly HttpClient _httpClient;
        private readonly ILogger<RagService> _logger;
        private readonly string _baseUrl;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public RagService(HttpClient httpClient, IConfiguration configuration, ILogger<RagService> logger, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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

                var response = await _httpClient.PostAsJsonAsync("/api/v1/chat", new
                {
                    question = request.Question,
                    document_ids = new List<string>(),
                    verbose = false
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
                using var content = new MultipartFormDataContent();

                // Đảm bảo Stream ở vị trí 0 - Đây là điều kiện tiên quyết để gửi dữ liệu
                if (fileStream.CanSeek) fileStream.Position = 0;
                
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                // Gửi file với key là "files" (không thêm ngoặc kép thủ công)
                // Python mong đợi files: List[UploadFile]
                content.Add(streamContent, "files", fileName); 

                // Gửi source_type vì Python có tham số này trong upload_files
                content.Add(new StringContent("wikipedia"), "source_type");

                // LƯU Ý: Không gửi chunk_size/chunk_overlap ở đây nếu Python 
                // chỉ nhận files và source_type trong tham số hàm upload_files.
                // Gửi dư field có thể khiến FastAPI báo lỗi 422 hoặc parse sai.

                var response = await _httpClient.PostAsync("api/v1/upload", content);
                
                var rawJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("RAW RESPONSE FROM RAG: {Raw}", rawJson);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetail = await response.Content.ReadAsStringAsync();
                    _logger.LogError("RAG Service trả về lỗi: {Detail}", errorDetail);
                    response.EnsureSuccessStatusCode();
                }

                var result = await response.Content.ReadFromJsonAsync<DocumentUploadResponse>();
                return result ?? throw new Exception("Response rỗng");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối RAG Service khi upload {File}", fileName);
                throw;
            }
        }

        public async Task<EditDocumentJobResponse> EditDocumentAsync(
            string documentId,
            Stream fileStream,
            string fileName,
            int chunkSize = 800,
            int chunkOverlap = 150)
        {
            try
            {
                _logger.LogInformation("Editing document {DocumentId} via RAG service", documentId);

                using var content = new MultipartFormDataContent();

                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "file", fileName);
                content.Add(new StringContent(chunkSize.ToString()), "chunk_size");
                content.Add(new StringContent(chunkOverlap.ToString()), "chunk_overlap");

                var response = await _httpClient.PutAsync($"/api/v1/documents/{documentId}/edit", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<EditDocumentJobResponse>();

                if (result == null)
                {
                    throw new Exception("Failed to deserialize edit response");
                }

                _logger.LogInformation("Document edit queued successfully: {DocumentId}", documentId);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while editing document {DocumentId}", documentId);
                throw new Exception($"Failed to edit document in RAG service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditDocumentAsync for {DocumentId}", documentId);
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

                var response = await _httpClient.GetAsync($"/api/v1/jobs/{jobId}");
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

        public async Task<GenerateNodeResponseDto> GenerateNodeAsync(string targetPerson, string filename, string htmlContent)
        {
            try
            {
                // Log đường dẫn file để em dễ kiểm tra trong thư mục Debug/Root
                var absolutePath = Path.GetFullPath(filename);
                _logger.LogInformation(">>> ĐANG XỬ LÝ FILE TẠI: {Path}", absolutePath);

                var graphRagBaseUrl = _configuration["GraphRAGService:BaseUrl"]?.TrimEnd('/');
                if (string.IsNullOrEmpty(graphRagBaseUrl)) graphRagBaseUrl = "http://localhost:8000";

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(5); 

                using var content = new MultipartFormDataContent();

                var fileBytes = Encoding.UTF8.GetBytes(htmlContent);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                content.Add(fileContent, "file", filename);

                content.Add(new StringContent(targetPerson), "target_person");
                // Thêm các tham số mặc định để FastAPI không báo lỗi thiếu Form
                content.Add(new StringContent("vietnam_history"), "preset");
                content.Add(new StringContent("true"), "use_original_prompt");

                var finalUrl = $"{graphRagBaseUrl}/upload";
                var response = await client.PostAsync(finalUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    // Đọc JSON trả về từ Python
                    var result = await response.Content.ReadFromJsonAsync<GenerateNodeResponseDto>();
                    
                    if (result != null)
                    {
                        // Giữ nguyên logic cũ: Trả về object result đã có JobId (nếu DTO đã có JsonPropertyName)
                        result.Success = true;
                        _logger.LogInformation(">>> NHẬN JOB ID TỪ PYTHON: {JobId}", result.JobId);
                        return result;
                    }
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                return new GenerateNodeResponseDto { Success = false, Message = $"GraphRAG error: {response.StatusCode} - {errorMsg}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối GraphRAG localhost");
                return new GenerateNodeResponseDto { Success = false, Message = ex.Message };
            }
        }

        private string SanitizeFileName(string name)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim('_');
        }

        public async Task<ChatResponse> GraphRagChatAsync(ChatRequest request)
        {
            try
            {
                // 1. LẤY ĐÚNG URL LOCALHOST (Port 8000)
                var graphRagUrl = _configuration["GraphRAGService:BaseUrl"]?.TrimEnd('/');
                if (string.IsNullOrEmpty(graphRagUrl)) graphRagUrl = "http://localhost:8000";

                // 2. DÙNG CLIENT SẠCH (KHÔNG DÙNG _httpClient CỦA CLASS)
                using var client = _httpClientFactory.CreateClient();
                
                _logger.LogInformation("Proxying Chat to: {Url}", $"{graphRagUrl}/chat");

                // 3. GỬI REQUEST
                var response = await client.PostAsJsonAsync($"{graphRagUrl}/chat", new 
                { 
                    question = request.Question,
                    // Đảm bảo key 'question' khớp với Pydantic QueryRequest bên Python
                });

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"FastAPI Error: {error}");
                }

                // 4. DESERIALIZE ĐÚNG FORMAT
                // Bên Python trả về QueryResponse(answer=...) nên ta cần lấy đúng field 'answer'
                var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                
                if (result != null)
                {
                    return result;
                }

                throw new Exception($"Empty response from GraphRAG");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GraphRagChatAsync");
                throw new Exception(ex.Message);
            }
        }

        public async Task<GraphRagJobStatusResponseDTO?> GetNodeStatusAsync(string jobId)
        {
            try
            {
                var graphRagBaseUrl = _configuration["GraphRAGService:BaseUrl"]?.TrimEnd('/');
                if (string.IsNullOrEmpty(graphRagBaseUrl)) 
                    graphRagBaseUrl = "https://sep490wikichatbotgraphragmodel-bcf9hvcecfc9edc8.japaneast-01.azurewebsites.net";

                using var client = _httpClientFactory.CreateClient();
                var url = $"{graphRagBaseUrl}/status/{jobId}";

                _logger.LogInformation(">>> ĐANG KIỂM TRA TRẠNG THÁI JOB: {Url}", url);

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GraphRagJobStatusResponseDTO>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy status từ GraphRAG cho JobId: {JobId}", jobId);
                return null;
            }
        }
    }
}

