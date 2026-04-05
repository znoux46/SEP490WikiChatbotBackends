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
    private readonly ILogger<GraphRagController> _logger;

    public GraphRagController(IRagService ragService, IChatHistoryService chatHistoryService, ILogger<GraphRagController> logger)
    {
        _ragService = ragService;
        _logger = logger;
        _chatHistoryService = chatHistoryService;
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

            var result = await _ragService.GraphRagChatAsync(request);

            result.AIModel = "GraphRAG"; // Indicate which model was used

            if (User.Identity?.IsAuthenticated == true)
            {
                result.SessionId = await _chatHistoryService.SaveChatHistoryWithContextAsync(request.Question, result.Answer, "GraphRAG", request.SessionId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GraphRAG chat");
            return StatusCode(500, new GraphRagChatResponseDto { Success = false, Error = ex.Message });
        }
    }
}
