using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;

namespace WikiChatbotBackends.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    #region User Management

    /// <summary>
    /// Get all users with pagination and filtering
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResultDto<AdminUserDto>>> GetAllUsers([FromQuery] UserQueryDto query)
    {
        try
        {
            var result = await _adminService.GetAllUsersAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<AdminUserDto>> GetUserById(int userId)
    {
        try
        {
            var user = await _adminService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = $"User with id {userId} not found" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update user information
    /// </summary>
    [HttpPut("users/{userId}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(int userId, [FromBody] UpdateUserDto dto)
    {
        try
        {
            var user = await _adminService.UpdateUserAsync(userId, dto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update user role
    /// </summary>
    [HttpPut("users/{userId}/role")]
    public async Task<ActionResult<AdminUserDto>> UpdateUserRole(int userId, [FromBody] UpdateUserRoleDto dto)
    {
        try
        {
            var user = await _adminService.UpdateUserRoleAsync(userId, dto.Role);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<ActionResult> DeleteUser(int userId)
    {
        try
        {
            // Get current admin ID to prevent self-deletion
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (currentUserIdClaim != null && int.TryParse(currentUserIdClaim.Value, out var currentUserId) && currentUserId == userId)
            {
                return BadRequest(new { message = "You cannot delete your own account" });
            }

            await _adminService.DeleteUserAsync(userId);
            return Ok(new { message = "User deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetStatistics()
    {
        try
        {
            var stats = await _adminService.GetStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get daily statistics for the last N days
    /// </summary>
    [HttpGet("stats/daily")]
    public async Task<ActionResult<List<DailyStatsDto>>> GetDailyStats([FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 90)
                return BadRequest(new { message = "Days must be between 1 and 90" });

            var stats = await _adminService.GetDailyStatsAsync(days);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily stats");
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Chat Session Management

    /// <summary>
    /// Get all chat sessions with pagination and filtering
    /// </summary>
    [HttpGet("chat-sessions")]
    public async Task<ActionResult<PagedResultDto<AdminChatSessionDto>>> GetAllChatSessions([FromQuery] ChatSessionQueryDto query)
    {
        try
        {
            var result = await _adminService.GetAllChatSessionsAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all chat sessions");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get chat session by ID
    /// </summary>
    [HttpGet("chat-sessions/{sessionId}")]
    public async Task<ActionResult<AdminChatSessionDto>> GetChatSessionById(int sessionId)
    {
        try
        {
            var session = await _adminService.GetChatSessionByIdAsync(sessionId);
            if (session == null)
                return NotFound(new { message = $"Chat session with id {sessionId} not found" });

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat session by id {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete chat session
    /// </summary>
    [HttpDelete("chat-sessions/{sessionId}")]
    public async Task<ActionResult> DeleteChatSession(int sessionId)
    {
        try
        {
            await _adminService.DeleteChatSessionAsync(sessionId);
            return Ok(new { message = "Chat session deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chat session {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete all chat sessions for a specific user
    /// </summary>
    [HttpDelete("users/{userId}/chat-sessions")]
    public async Task<ActionResult> DeleteAllUserChatSessions(int userId)
    {
        try
        {
            await _adminService.DeleteAllUserChatSessionsAsync(userId);
            return Ok(new { message = "All chat sessions for user deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all chat sessions for user {UserId}", userId);
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    /// <summary>
    /// Health check endpoint for admin panel
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

