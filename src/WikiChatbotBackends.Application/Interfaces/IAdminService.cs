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
    Task<int> CreateUserAsync(CreateUserDto createUser);

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

    Task<WikipediaGenerateNodeResponseDto> GenerateWikipediaNodeAsync(WikipediaGenerateNodeRequestDto request);

    // Person Summary from Wikipedia
    Task<PersonSummaryResponseDto> GetPersonSummaryAsync(PersonSummaryRequestDto request);

    /// <summary>
    /// Start Wikipedia chunking job: extract content, queue for async processing
    /// </summary>
    Task<WikipediaChunkingResponseDto> StartWikipediaChunkingAsync(WikipediaChunkingRequestDto request);

    /// <summary>
    /// Get status of Wikipedia chunking job
    /// </summary>
    Task<WikipediaJobStatusResponseDto?> GetWikipediaJobStatusAsync(string jobId);
}

