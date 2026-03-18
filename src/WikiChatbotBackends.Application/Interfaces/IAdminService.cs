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
    Task<List<TimeSeriesStatsDto>> GetTimeSeriesStatsAsync(TimeGrouping grouping, int days = 30);
    Task<List<UserGrowthStatsDto>> GetUserGrowthStatsAsync(int days = 30);

    // Active Users
    Task<PagedResultDto<ActiveUserDto>> GetTopActiveUsersAsync(TopActiveUsersQueryDto query);

    // Chat Session Management
    Task<PagedResultDto<AdminChatSessionDto>> GetAllChatSessionsAsync(ChatSessionQueryDto query);
    Task<AdminChatSessionDto?> GetChatSessionByIdAsync(Guid sessionId);
    Task<bool> DeleteChatSessionAsync(Guid sessionId);
    Task<bool> DeleteAllUserChatSessionsAsync(int userId);

    // Document Management - Wikipedia Import
    Task<AddDocumentFromWikipediaResponseDto> AddDocumentFromWikipediaAsync(AddDocumentFromWikipediaRequestDto request);
    
    // Document Management - Wikipedia Edit
    Task<AddDocumentFromWikipediaResponseDto> EditDocumentFromWikipediaAsync(AddDocumentFromWikipediaRequestDto request);
}

