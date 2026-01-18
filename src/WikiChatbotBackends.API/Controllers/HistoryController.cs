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

    // Session endpoints
    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<SessionSummaryDto>>> GetUserSessions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        var sessions = await _chatHistoryService.GetUserSessionsAsync(userId);
        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId}")]
    public async Task<ActionResult<ChatSessionDto>> GetSession(int sessionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            var session = await _chatHistoryService.GetSessionByIdAsync(userId, sessionId);
            return Ok(session);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<ChatSessionDto>> CreateSession([FromBody] CreateChatSessionDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            var session = await _chatHistoryService.CreateSessionAsync(userId, dto);
            return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, session);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("sessions/{sessionId}")]
    public async Task<ActionResult<ChatSessionDto>> UpdateSession(int sessionId, [FromBody] UpdateChatSessionDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            var session = await _chatHistoryService.UpdateSessionAsync(userId, sessionId, dto);
            return Ok(session);
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

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int sessionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            await _chatHistoryService.DeleteSessionAsync(userId, sessionId);
            return NoContent();
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

    [HttpDelete("sessions/clear-all")]
    public async Task<IActionResult> ClearAllSessions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            await _chatHistoryService.ClearAllSessionsAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // History endpoints
    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<ActionResult<IEnumerable<ChatHistoryDto>>> GetSessionHistory(int sessionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            var history = await _chatHistoryService.GetSessionHistoryAsync(userId, sessionId);
            return Ok(history);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ChatHistoryDto>> CreateChatHistory([FromBody] CreateChatHistoryDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            var history = await _chatHistoryService.CreateChatHistoryAsync(userId, dto);
            return CreatedAtAction(nameof(GetSessionHistory), new { sessionId = dto.SessionId }, history);
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

    [HttpPut("messages/{id}")]
    public async Task<ActionResult<ChatHistoryDto>> UpdateChatHistory(int id, [FromBody] UpdateChatHistoryDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            var history = await _chatHistoryService.UpdateChatHistoryAsync(userId, id, dto);
            return Ok(history);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("messages/{id}")]
    public async Task<IActionResult> DeleteChatHistory(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            await _chatHistoryService.DeleteChatHistoryAsync(userId, id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}