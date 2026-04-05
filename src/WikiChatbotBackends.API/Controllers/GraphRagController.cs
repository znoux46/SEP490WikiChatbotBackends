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
        try
        {
            _logger.LogInformation("GraphRAG chat request: {Question} (SessionId: {SessionId})", request.Question, request.SessionId);

            // Rewrite question
            request.Question = await _questionRewriteService.RewriteQuestion(request.Question, request.SessionId);
            // Call GraphRAG service
            var result = await _ragService.GraphRagChatAsync(request);

            result.AIModel = "GraphRAG"; // Indicate which model was used

            // Save history
            result.SessionId = await _chatHistoryService.SaveChatHistoryWithContextAsync(request.Question, result.Answer, "GraphRAG",result.ActivePerson, request.SessionId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GraphRAG chat");
            return StatusCode(500, new GraphRagChatResponseDto { Success = false, Error = ex.Message });
        }
    }
}
