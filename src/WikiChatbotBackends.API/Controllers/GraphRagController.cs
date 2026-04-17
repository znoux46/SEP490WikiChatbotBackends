using Microsoft.AspNetCore.Mvc;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/graphrag")]
public class GraphRagController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IQuestionRewriteService _questionRewriteService;
    private readonly ILogger<GraphRagController> _logger;

    public GraphRagController(IRagService ragService,
        IChatHistoryService chatHistoryService,
        IQuestionRewriteService questionRewriteService,
        ILogger<GraphRagController> logger)
    {
        _ragService = ragService;
        _logger = logger;
        _chatHistoryService = chatHistoryService;
        _questionRewriteService = questionRewriteService;
    }

    /// <summary>
    /// GraphRAG chat endpoint - proxies to model /chat, saves to chat history if SessionId provided
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        string originalQuestion = request.Question;
        
        try
        {
            string displayQuestion = request.Question;
            
            _logger.LogInformation("GraphRAG chat request: {Question} (SessionId: {SessionId})", request.Question, request.SessionId);

            // Rewrite question
            var rewrittenQuestion = await _questionRewriteService.RewriteQuestion(originalQuestion, request.SessionId);
            request.Question = rewrittenQuestion;            
            
            // Call GraphRAG service
            var result = await _ragService.GraphRagChatAsync(request);

            result.Question = originalQuestion;
            result.AIModel = "GraphRAG"; // Indicate which model was used

            // Save history
            result.SessionId = await _chatHistoryService.SaveChatHistoryWithContextAsync(request.Question, result.Answer, "GraphRAG",result.ActivePerson, request.SessionId);

            // 4. Lưu history và lấy SessionId chuẩn (Xử lý được cả vụ Anonymous)
            var finalSessionId = await _chatHistoryService.SaveChatHistoryWithContextAsync(
                result.Question, 
                result.Answer, 
                "GraphRAG",
                result.ActivePerson, 
                request.SessionId);

            // Cập nhật lại SessionId vào kết quả trả về
            result.SessionId = finalSessionId;

            return Ok(result);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error in GraphRAG chat");
            // return StatusCode(500, new GraphRagChatResponseDto { Success = false, Error = ex.Message });

            _logger.LogError(ex, "Error in GraphRAG chat");
            // Đảm bảo DTO trả về đồng nhất để Frontend không bị parse lỗi
            return StatusCode(500, new ChatResponse { 
                Answer = "Có lỗi xảy ra khi xử lý GraphRAG: " + ex.Message,
                Question = originalQuestion,
                SessionId = request.SessionId.ToString()
            });
        }
    }

    [HttpGet("node-status/{jobId}")]
    public async Task<IActionResult> GetNodeStatus(string jobId)
    {
        var status = await _ragService.GetNodeStatusAsync(jobId);
        
        if (status == null)
        {
            return NotFound(new { message = $"Không tìm thấy Job ID: {jobId}" });
        }

        // Trả về nguyên văn object DTO khớp hoàn toàn với response từ Python
        return Ok(status);
    }
}
