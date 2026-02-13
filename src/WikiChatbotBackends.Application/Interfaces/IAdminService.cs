using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IAdminService
{
    // User Management
    Task<PagedResultDto<AdminUserDto>> GetAllUsersAsync(UserQueryDto query);
    Task<AdminUserDto?> GetUserByIdAsync(int userId);
    Task<AdminUserDto> UpdateUserAsync(int userId, UpdateUserDto dto);
    Task<bool> DeleteUserAsync(int userId);
    Task<AdminUserDto> UpdateUserRoleAsync(int userId, string role);

    // Statistics
    Task<AdminStatsDto> GetStatisticsAsync();
    Task<List<DailyStatsDto>> GetDailyStatsAsync(int days = 7);

    // Chat Session Management
    Task<PagedResultDto<AdminChatSessionDto>> GetAllChatSessionsAsync(ChatSessionQueryDto query);
    Task<AdminChatSessionDto?> GetChatSessionByIdAsync(int sessionId);
    Task<bool> DeleteChatSessionAsync(int sessionId);
    Task<bool> DeleteAllUserChatSessionsAsync(int userId);
}

