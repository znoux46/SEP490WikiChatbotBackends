using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuestionController : ControllerBase
    {
        private readonly IRagService _ragService;
        private readonly IChatHistoryService _chatHistoryService;
        private readonly ILogger<QuestionController> _logger;

        public QuestionController(
            IRagService ragService,
            IChatHistoryService chatHistoryService,
            ILogger<QuestionController> logger)
        {
            _ragService = ragService;
            _chatHistoryService = chatHistoryService;
            _logger = logger;
        }

        [AllowAnonymous] // Bỏ qua authorize
        [HttpPost]
        public async Task<ActionResult<ChatResponse>> AskQuestion([FromBody] ChatRequest request)
        {
            // 1. Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { message = "Question cannot be empty" });

            // 2. Gọi RAG lấy câu trả lời
            var response = await _ragService.ChatAsync(request);

            // 3. Lưu lịch sử (Service tự bóc tách HttpContext để lấy User/Session)
            // Bạn không cần lo về Try-Catch ở đây vì trong Service đã xử lý rồi
            await _chatHistoryService.SaveChatHistoryWithContextAsync(request.Question, response.Answer);

            return Ok(response);
        }

        /// <summary>
        /// Search for relevant document chunks without generating an answer
        /// </summary>
        /// <param name="request">Search request with query and parameters</param>
        /// <returns>Search results with matching chunks</returns>
        [HttpPost("search")]
        [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SearchResponse>> Search([FromBody] SearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { message = "Query cannot be empty" });
                }

                _logger.LogInformation("Searching for: {Query}", request.Query);

                var response = await _ragService.SearchAsync(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for: {Query}", request.Query);
                return StatusCode(500, new { message = "An error occurred while searching", error = ex.Message });
            }
        }

        /// <summary>
        /// Upload a document to the RAG system for processing
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <param name="chunkSize">Size of chunks (default: 800)</param>
        /// <param name="chunkOverlap">Overlap between chunks (default: 150)</param>
        /// <returns>Upload response with job information</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DocumentUploadResponse>> UploadDocument(
            IFormFile file,
            [FromQuery] int chunkSize = 800,
            [FromQuery] int chunkOverlap = 150)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Check file size (max 50MB)
                if (file.Length > 50 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File size exceeds 50MB limit" });
                }

                _logger.LogInformation("Uploading document: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

                using var stream = file.OpenReadStream();
                var response = await _ragService.UploadDocumentAsync(stream, file.FileName, chunkSize, chunkOverlap);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document: {FileName}", file?.FileName);
                return StatusCode(500, new { message = "An error occurred while uploading the document", error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of all documents in the RAG system
        /// </summary>
        /// <param name="skip">Number of documents to skip (pagination)</param>
        /// <param name="limit">Maximum number of documents to return</param>
        /// <returns>List of documents</returns>
        [HttpGet("documents")]
        [ProducesResponseType(typeof(List<DocumentInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DocumentInfo>>> GetDocuments(
            [FromQuery] int skip = 0,
            [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("Fetching documents (skip: {Skip}, limit: {Limit})", skip, limit);

                var documents = await _ragService.GetDocumentsAsync(skip, limit);

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents");
                return StatusCode(500, new { message = "An error occurred while fetching documents", error = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed information about a specific document
        /// </summary>
        /// <param name="documentId">ID of the document</param>
        /// <returns>Document information</returns>
        [HttpGet("documents/{documentId}")]
        [ProducesResponseType(typeof(DocumentInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DocumentInfo>> GetDocument(string documentId)
        {
            try
            {
                _logger.LogInformation("Fetching document: {DocumentId}", documentId);

                var document = await _ragService.GetDocumentByIdAsync(documentId);

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document: {DocumentId}", documentId);
                
                if (ex.Message.Contains("404") || ex.Message.Contains("not found"))
                {
                    return NotFound(new { message = $"Document not found: {documentId}" });
                }

                return StatusCode(500, new { message = "An error occurred while fetching the document", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a document from the RAG system
        /// </summary>
        /// <param name="documentId">ID of the document to delete</param>
        /// <returns>Success status</returns>
        [HttpDelete("documents/{documentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteDocument(string documentId)
        {
            try
            {
                _logger.LogInformation("Deleting document: {DocumentId}", documentId);

                var success = await _ragService.DeleteDocumentAsync(documentId);

                if (!success)
                {
                    return NotFound(new { message = $"Document not found: {documentId}" });
                }

                return Ok(new { message = $"Document {documentId} deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
                return StatusCode(500, new { message = "An error occurred while deleting the document", error = ex.Message });
            }
        }

        /// <summary>
        /// Get the status of a document processing job
        /// </summary>
        /// <param name="jobId">ID of the job</param>
        /// <returns>Job status information</returns>
        [HttpGet("status/{jobId}")]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<JobStatusResponse>> GetJobStatus(string jobId)
        {
            try
            {
                _logger.LogInformation("Fetching job status: {JobId}", jobId);

                var status = await _ragService.GetJobStatusAsync(jobId);

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job status: {JobId}", jobId);
                
                if (ex.Message.Contains("404") || ex.Message.Contains("not found"))
                {
                    return NotFound(new { message = $"Job not found: {jobId}" });
                }

                return StatusCode(500, new { message = "An error occurred while fetching job status", error = ex.Message });
            }
        }

        /// <summary>
        /// Check if the RAG service is healthy and available
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult> HealthCheck()
        {
            try
            {
                var isHealthy = await _ragService.HealthCheckAsync();

                if (isHealthy)
                {
                    return Ok(new { status = "healthy", service = "RAG Service" });
                }

                return StatusCode(503, new { status = "unhealthy", service = "RAG Service" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking RAG service health");
                return StatusCode(503, new { status = "unhealthy", service = "RAG Service", error = ex.Message });
            }
        }
    }
}
