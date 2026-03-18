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
    private readonly ICategoryService _categoryService;
    private readonly IDetailService _detailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ICategoryService categoryService, IDetailService detailService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _categoryService = categoryService;
        _detailService = detailService;
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

    /// <summary>
    /// Get time series statistics (messages/sessions grouped by day/week/month)
    /// </summary>
    [HttpGet("stats/time-series")]
    public async Task<ActionResult<List<TimeSeriesStatsDto>>> GetTimeSeriesStats(
        [FromQuery] TimeGrouping grouping = TimeGrouping.Day,
        [FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days must be between 1 and 365" });

            var stats = await _adminService.GetTimeSeriesStatsAsync(grouping, days);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time series stats");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user growth statistics over time
    /// </summary>
    [HttpGet("stats/user-growth")]
    public async Task<ActionResult<List<UserGrowthStatsDto>>> GetUserGrowthStats([FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days must be between 1 and 365" });

            var stats = await _adminService.GetUserGrowthStatsAsync(days);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user growth stats");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get top active users
    /// </summary>
    [HttpGet("users/top-active")]
    public async Task<ActionResult<PagedResultDto<ActiveUserDto>>> GetTopActiveUsers([FromQuery] TopActiveUsersQueryDto query)
    {
        try
        {
            var result = await _adminService.GetTopActiveUsersAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top active users");
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
    public async Task<ActionResult<AdminChatSessionDto>> GetChatSessionById(Guid sessionId)
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
    public async Task<ActionResult> DeleteChatSession(Guid sessionId)
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

    #region Category Management

    /// <summary>
    /// Get all categories for admin
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        try
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create new category
    /// </summary>
    [HttpPost("categories")]
    public async Task<ActionResult<Guid>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name is required" });

            var id = await _categoryService.CreateAsync(dto);
            return Ok(new { id, message = "Category created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update category
    /// </summary>
    [HttpPut("categories/{id}")]
    public async Task<ActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        try
        {
            await _categoryService.UpdateAsync(id, dto);
            return Ok(new { message = "Category updated successfully" });
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
            _logger.LogError(ex, "Error updating category {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete category
    /// </summary>
    [HttpDelete("categories/{id}")]
    public async Task<ActionResult> DeleteCategory(Guid id)
    {
        try
        {
            await _categoryService.DeleteAsync(id);
            return Ok(new { message = "Category deleted successfully" });
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
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Detail Management

    /// <summary>
    /// Get all details for admin (temporary - use category/details later)
    /// </summary>
    [HttpGet("details")]
    public async Task<ActionResult<List<DetailDto>>> GetDetails()
    {
        try
        {
            var categories = await _categoryService.GetAllAsync();
            var allDetails = new List<DetailDto>();
            foreach (var cat in categories)
            {
                var details = await _detailService.GetByCategoryIdAsync(cat.Id);
                allDetails.AddRange(details);
            }
            return Ok(allDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting details");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create new detail
    /// </summary>
    [HttpPost("details")]
    public async Task<ActionResult<Guid>> CreateDetail([FromBody] CreateDetailDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Title is required" });

            var id = await _detailService.CreateAsync(dto);
            return Ok(new { id, message = "Detail created successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating detail");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update detail
    /// </summary>
    [HttpPut("details/{id}")]
    public async Task<ActionResult> UpdateDetail(Guid id, [FromBody] UpdateDetailDto dto)
    {
        try
        {
            await _detailService.UpdateAsync(id, dto);
            return Ok(new { message = "Detail updated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating detail {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete detail
    /// </summary>
    [HttpDelete("details/{id}")]
    public async Task<ActionResult> DeleteDetail(Guid id)
    {
        try
        {
            await _detailService.DeleteAsync(id);
            return Ok(new { message = "Detail deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting detail {Id}", id);
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

    #region Document Management - Wikipedia Import

    /// <summary>
    /// Add a document from Wikipedia by specifying a historical figure's name
    /// </summary>
    /// <param name="request">Request containing the name of the historical figure</param>
    /// <returns>Information about the imported document</returns>
    [HttpPost("documents/wikipedia")]
    public async Task<ActionResult<AddDocumentFromWikipediaResponseDto>> AddDocumentFromWikipedia(
        [FromBody] AddDocumentFromWikipediaRequestDto request)
    {
        try
        {
            _logger.LogInformation("Adding document from Wikipedia: {Name}", request.Name);

            var result = await _adminService.AddDocumentFromWikipediaAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Successfully added document from Wikipedia: {Title}", result.WikipediaTitle);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document from Wikipedia: {Name}", request.Name);
            return BadRequest(new AddDocumentFromWikipediaResponseDto
            {
                Success = false,
                Message = $"Error importing document: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Edit a document from Wikipedia - hard deletes old document and chunks, then re-imports fresh content
    /// </summary>
    /// <param name="request">Same request format as AddDocumentFromWikipedia</param>
    /// <returns>Same response format as AddDocumentFromWikipedia</returns>
    [HttpPost("documents/wikipedia/edit")]
    public async Task<ActionResult<AddDocumentFromWikipediaResponseDto>> EditDocumentFromWikipedia(
        [FromBody] AddDocumentFromWikipediaRequestDto request)
    {
        try
        {
            _logger.LogInformation("Editing document from Wikipedia: {Name}", request.Name);

            var result = await _adminService.EditDocumentFromWikipediaAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Successfully edited document from Wikipedia: {Title}", result.WikipediaTitle);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing document from Wikipedia: {Name}", request.Name);
            return BadRequest(new AddDocumentFromWikipediaResponseDto
            {
                Success = false,
                Message = $"Error updating document: {ex.Message}"
            });
        }
    }

    #endregion
}

