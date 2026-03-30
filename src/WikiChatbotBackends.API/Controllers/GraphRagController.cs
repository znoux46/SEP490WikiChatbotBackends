using Microsoft.AspNetCore.Mvc;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/graphrag")]
public class GraphRagController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ILogger<GraphRagController> _logger;

    public GraphRagController(IRagService ragService, ILogger<GraphRagController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// GraphRAG chat endpoint - proxies to model /chat, saves to chat history if SessionId provided
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<GraphRagChatResponseDto>> Chat([FromBody] GraphRagChatRequestDto request)
    {
        try
        {
            _logger.LogInformation("GraphRAG chat request: {Question} (SessionId: {SessionId})", request.Question, request.SessionId);

            var result = await _ragService.GraphRagChatAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            // TODO: Optionally save to ChatHistory using IChatHistoryService (requires injection + session validation)
            // Similar to QuestionController logic

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GraphRAG chat");
            return StatusCode(500, new GraphRagChatResponseDto { Success = false, Error = ex.Message });
        }
    }
}
