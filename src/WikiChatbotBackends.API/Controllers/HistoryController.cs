using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/history")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly IChatHistoryService _chatHistoryService;

    public HistoryController(IChatHistoryService chatHistoryService)
    {
        _chatHistoryService = chatHistoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionSummaryDto>>> GetUserSessions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        var sessions = await _chatHistoryService.GetUserSessionsAsync(userId);
        return Ok(sessions);
    }

    [HttpGet("{sessionId}")]
    public async Task<ActionResult<IEnumerable<ChatHistoryDto>>> GetSessionHistory(string sessionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        var history = await _chatHistoryService.GetSessionHistoryAsync(userId, sessionId);
        return Ok(history);
    }

    [HttpPost]
    public async Task<ActionResult<ChatHistoryDto>> CreateChatHistory([FromBody] CreateChatHistoryDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var history = await _chatHistoryService.CreateChatHistoryAsync(userId, dto);
            return CreatedAtAction(nameof(GetSessionHistory), new { sessionId = dto.SessionId }, history);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ChatHistoryDto>> UpdateChatHistory(int id, [FromBody] UpdateChatHistoryDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var history = await _chatHistoryService.UpdateChatHistoryAsync(userId, id, dto);
            return Ok(history);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            await _chatHistoryService.DeleteSessionAsync(userId, sessionId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllHistory()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            await _chatHistoryService.ClearAllHistoryAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
