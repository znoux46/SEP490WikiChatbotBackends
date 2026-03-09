using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IChatHistoryRepository _chatHistoryRepository;
    private readonly IRagService _ragService;
    private readonly IWikipediaService _wikipediaService;

    public AdminService(
        IUserRepository userRepository,
        IChatSessionRepository chatSessionRepository,
        IChatHistoryRepository chatHistoryRepository,
        IRagService ragService,
        IWikipediaService wikipediaService)
    {
        _userRepository = userRepository;
        _chatSessionRepository = chatSessionRepository;
        _chatHistoryRepository = chatHistoryRepository;
        _ragService = ragService;
        _wikipediaService = wikipediaService;
    }

    #region User Management

    public async Task<PagedResultDto<AdminUserDto>> GetAllUsersAsync(UserQueryDto query)
    {
        // Build predicate for filtering
        Func<User, bool>? predicate = null;
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            predicate = u => 
                u.Username.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                u.FullName.ToLower().Contains(searchTerm);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role;
            if (predicate != null)
            {
                var existingPredicate = predicate;
                predicate = u => existingPredicate(u) && u.Role == role;
            }
            else
            {
                predicate = u => u.Role == role;
            }
        }

        // Get total count
        int totalCount;
        if (predicate != null)
        {
            totalCount = await _userRepository.CountUsersAsync(u => predicate(u));
        }
        else
        {
            totalCount = await _userRepository.CountUsersAsync();
        }

        // Get users with sorting
        var users = await _userRepository.GetAllAsync();
        var filteredUsers = predicate != null ? users.Where(predicate) : users;

        // Apply sorting
        var sortedUsers = query.SortBy?.ToLower() switch
        {
            "username" => query.SortDescending 
                ? filteredUsers.OrderByDescending(u => u.Username) 
                : filteredUsers.OrderBy(u => u.Username),
            "email" => query.SortDescending 
                ? filteredUsers.OrderByDescending(u => u.Email) 
                : filteredUsers.OrderBy(u => u.Email),
            "role" => query.SortDescending 
                ? filteredUsers.OrderByDescending(u => u.Role) 
                : filteredUsers.OrderBy(u => u.Role),
            "updatedat" => query.SortDescending 
                ? filteredUsers.OrderByDescending(u => u.UpdatedAt) 
                : filteredUsers.OrderBy(u => u.UpdatedAt),
            _ => filteredUsers.OrderByDescending(u => u.CreatedAt)
        };

        // Apply pagination
        var items = sortedUsers
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToList();

        return new PagedResultDto<AdminUserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new AdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<AdminUserDto> UpdateUserAsync(int userId, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            // Check if email is already used by another user
            var existingEmail = await _userRepository.ExistsByEmailAsync(dto.Email, userId);
            if (existingEmail)
                throw new InvalidOperationException("Email is already in use");

            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            // Validate role
            var validRoles = new[] { "User", "Admin", "Moderator" };
            if (!validRoles.Contains(dto.Role))
                throw new InvalidOperationException($"Invalid role. Valid roles are: {string.Join(", ", validRoles)}");

            user.Role = dto.Role;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return new AdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        await _userRepository.DeleteAsync(user);
        return true;
    }

    public async Task<AdminUserDto> UpdateUserRoleAsync(int userId, string role)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        // Validate role
        var validRoles = new[] { "User", "Admin", "Moderator" };
        if (!validRoles.Contains(role))
            throw new InvalidOperationException($"Invalid role. Valid roles are: {string.Join(", ", validRoles)}");

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return new AdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    #endregion

    #region Statistics

    public async Task<AdminStatsDto> GetStatisticsAsync()
    {
        var (totalUsers, totalAdmins, totalRegularUsers) = await _userRepository.GetUserStatisticsAsync();
        var totalChatSessions = await _chatSessionRepository.CountChatSessionsAsync();
        var totalChatMessages = await _chatHistoryRepository.CountChatHistoriesAsync();

        return new AdminStatsDto
        {
            TotalUsers = totalUsers,
            TotalChatSessions = totalChatSessions,
            TotalChatMessages = totalChatMessages,
            TotalAdmins = totalAdmins,
            TotalRegularUsers = totalRegularUsers
        };
    }

    public async Task<List<DailyStatsDto>> GetDailyStatsAsync(int days = 7)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var dailyStats = new List<DailyStatsDto>();

        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var nextDate = date.AddDays(1);

            var newUsers = await _userRepository.GetNewUsersCountAsync(date, nextDate);
            var newChatSessions = await _chatSessionRepository.GetNewChatSessionsCountAsync(date, nextDate);
            var newMessages = await _chatHistoryRepository.GetNewMessagesCountAsync(date, nextDate);

            dailyStats.Add(new DailyStatsDto
            {
                Date = date,
                NewUsers = newUsers,
                NewChatSessions = newChatSessions,
                NewMessages = newMessages
            });
        }

        return dailyStats;
    }

    #endregion

    #region Time Series Statistics

    public async Task<List<TimeSeriesStatsDto>> GetTimeSeriesStatsAsync(TimeGrouping grouping, int days = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var endDate = DateTime.UtcNow.Date;

        return await _chatHistoryRepository.GetTimeSeriesStatsAsync(startDate, endDate, grouping);
    }

    public async Task<List<UserGrowthStatsDto>> GetUserGrowthStatsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var endDate = DateTime.UtcNow.Date;

        return await _userRepository.GetUserGrowthStatsAsync(startDate, endDate);
    }

    #endregion

    #region Active Users

    public async Task<PagedResultDto<ActiveUserDto>> GetTopActiveUsersAsync(TopActiveUsersQueryDto query)
    {
        var startDate = query.StartDate ?? DateTime.UtcNow.Date.AddDays(-30);
        var endDate = query.EndDate ?? DateTime.UtcNow.Date.AddDays(1);

        // Get all users with their activity
        var users = await _userRepository.GetAllAsync();
        
        // Get message counts by user
        var messageCounts = await _chatHistoryRepository.GetMessageCountByUserAsync(startDate, endDate);
        
        // Get session counts by user
        var sessionCounts = await _userRepository.GetSessionCountByUserAsync(startDate, endDate);

        // Build active user list
        var activeUsers = users.Select(u => new ActiveUserDto
        {
            UserId = u.Id,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            TotalQuestions = messageCounts.GetValueOrDefault(u.Id, 0),
            TotalSessions = sessionCounts.GetValueOrDefault(u.Id, 0),
            TotalDocuments = 0, // RAG service doesn't track per-user documents in local DB
            LastActiveAt = u.UpdatedAt
        }).Where(u => u.TotalQuestions > 0 || u.TotalSessions > 0 || u.TotalDocuments > 0).ToList();

        // Apply sorting
        var sortedUsers = query.SortBy?.ToLower() switch
        {
            "totalquestions" => query.SortDescending
                ? activeUsers.OrderByDescending(u => u.TotalQuestions)
                : activeUsers.OrderBy(u => u.TotalQuestions),
            "totaldocuments" => query.SortDescending
                ? activeUsers.OrderByDescending(u => u.TotalDocuments)
                : activeUsers.OrderBy(u => u.TotalDocuments),
            "totalsessions" => query.SortDescending
                ? activeUsers.OrderByDescending(u => u.TotalSessions)
                : activeUsers.OrderBy(u => u.TotalSessions),
            _ => query.SortDescending
                ? activeUsers.OrderByDescending(u => u.TotalQuestions)
                : activeUsers.OrderBy(u => u.TotalQuestions)
        };

        var totalCount = sortedUsers.Count();

        // Apply pagination
        var items = sortedUsers
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResultDto<ActiveUserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    #endregion

    #region Chat Session Management

    public async Task<PagedResultDto<AdminChatSessionDto>> GetAllChatSessionsAsync(ChatSessionQueryDto query)
    {
        // Get all sessions with user included
        var allSessions = await _chatSessionRepository.GetChatSessionsAsync(
            includeUser: true);

        // Apply filters
        var filteredSessions = allSessions.AsEnumerable();
        
        if (query.UserId.HasValue)
        {
            filteredSessions = filteredSessions.Where(s => s.UserId == query.UserId.Value);
        }

        if (query.StartDate.HasValue)
        {
            filteredSessions = filteredSessions.Where(s => s.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            filteredSessions = filteredSessions.Where(s => s.CreatedAt <= query.EndDate.Value);
        }

        // Apply sorting
        var sortedSessions = query.SortBy?.ToLower() switch
        {
            "sessionname" => query.SortDescending 
                ? filteredSessions.OrderByDescending(s => s.SessionName) 
                : filteredSessions.OrderBy(s => s.SessionName),
            "userid" => query.SortDescending 
                ? filteredSessions.OrderByDescending(s => s.UserId) 
                : filteredSessions.OrderBy(s => s.UserId),
            "updatedat" => query.SortDescending 
                ? filteredSessions.OrderByDescending(s => s.UpdatedAt) 
                : filteredSessions.OrderBy(s => s.UpdatedAt),
            _ => filteredSessions.OrderByDescending(s => s.CreatedAt)
        };

        var totalCount = sortedSessions.Count();

        // Apply pagination
        var items = sortedSessions
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new AdminChatSessionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User?.Username ?? "Unknown",
                SessionId = s.SessionId,
                SessionName = s.SessionName,
                MessageCount = s.ChatHistories?.Count ?? 0,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToList();

        return new PagedResultDto<AdminChatSessionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<AdminChatSessionDto?> GetChatSessionByIdAsync(int sessionId)
    {
        var session = await _chatSessionRepository.GetChatSessionWithUserAsync(sessionId);
        if (session == null) return null;

        return new AdminChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Username = session.User?.Username ?? "Unknown",
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            MessageCount = session.ChatHistories?.Count ?? 0,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<bool> DeleteChatSessionAsync(int sessionId)
    {
        var session = await _chatSessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Chat session with id {sessionId} not found");

        await _chatSessionRepository.DeleteAsync(session);
        return true;
    }

    public async Task<bool> DeleteAllUserChatSessionsAsync(int userId)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        await _chatSessionRepository.DeleteAllUserChatSessionsAsync(userId);
        return true;
    }

    #endregion

    #region Document Management - Wikipedia Import

    public async Task<AddDocumentFromWikipediaResponseDto> AddDocumentFromWikipediaAsync(AddDocumentFromWikipediaRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new AddDocumentFromWikipediaResponseDto
            {
                Success = false,
                Message = "Name is required"
            };
        }

        try
        {
            // Step 1: Fetch article from Wikipedia API using the Wikipedia service
            var wikipediaData = await _wikipediaService.GetArticleSummaryAsync(request.Name);
            
            if (wikipediaData == null)
            {
                return new AddDocumentFromWikipediaResponseDto
                {
                    Success = false,
                    Message = $"Wikipedia article not found for: {request.Name}"
                };
            }

            // Step 2: Create document content from Wikipedia data
            var title = !string.IsNullOrWhiteSpace(request.CustomTitle) ? request.CustomTitle : wikipediaData.Title;
            var content = BuildWikipediaContent(wikipediaData);
            
            // Step 3: Convert content to stream and upload to RAG service
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
            memoryStream.Position = 0;

            var fileName = $"{SanitizeFileName(title)}.txt";
            var uploadResponse = await _ragService.UploadDocumentAsync(
                memoryStream, 
                fileName, 
                request.ChunkSize, 
                request.ChunkOverlap);

            // Step 4: Return success response
            var documentId = uploadResponse.Results.FirstOrDefault()?.DocumentId;
            var jobId = uploadResponse.Results.FirstOrDefault()?.JobId;

            return new AddDocumentFromWikipediaResponseDto
            {
                Success = true,
                Message = "Document imported successfully from Wikipedia",
                DocumentId = documentId,
                JobId = jobId,
                WikipediaTitle = wikipediaData.Title,
                WikipediaExtract = wikipediaData.Extract,
                WikipediaUrl = wikipediaData.ContentUrls?.Desktop?.Page
            };
        }
        catch (HttpRequestException ex)
        {
            return new AddDocumentFromWikipediaResponseDto
            {
                Success = false,
                Message = $"Failed to connect to Wikipedia: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new AddDocumentFromWikipediaResponseDto
            {
                Success = false,
                Message = $"Error importing document: {ex.Message}"
            };
        }
    }

    private string BuildWikipediaContent(WikipediaSummaryResponse wikiData)
    {
        var sb = new System.Text.StringBuilder();
        
        // Add title
        sb.AppendLine($"# {wikiData.Title}");
        sb.AppendLine();

        // Add description if available
        if (!string.IsNullOrWhiteSpace(wikiData.Description))
        {
            sb.AppendLine($"## {wikiData.Description}");
            sb.AppendLine();
        }

        // Add main content/extract
        if (!string.IsNullOrWhiteSpace(wikiData.Extract))
        {
            sb.AppendLine("## Content");
            sb.AppendLine();
            sb.AppendLine(wikiData.Extract);
            sb.AppendLine();
        }

        // Add metadata
        sb.AppendLine("## Metadata");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(wikiData.Timestamp))
        {
            sb.AppendLine($"- Last modified: {wikiData.Timestamp}");
        }
        if (wikiData.ContentUrls?.Desktop?.Page != null)
        {
            sb.AppendLine($"- Wikipedia URL: {wikiData.ContentUrls.Desktop.Page}");
        }

        return sb.ToString();
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return sanitized.Replace(" ", "_");
    }

    #endregion
}
