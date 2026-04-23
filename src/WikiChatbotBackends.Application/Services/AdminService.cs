
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentQueueService? _documentQueueService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUserRepository userRepository,
        IChatSessionRepository chatSessionRepository,
        IChatHistoryRepository chatHistoryRepository,
        IRagService ragService,
        IWikipediaService wikipediaService,
        IDocumentRepository documentRepository,
        ILogger<AdminService> logger,
        IDocumentQueueService? documentQueueService = null)
    {
        _userRepository = userRepository;
        _chatSessionRepository = chatSessionRepository;
        _chatHistoryRepository = chatHistoryRepository;
        _ragService = ragService;
        _wikipediaService = wikipediaService;
        _documentRepository = documentRepository;
        _documentQueueService = documentQueueService;
        _logger = logger;
    }

    #region User Management

    public async Task<PagedResultDto<AdminUserDto>> GetAllUsersAsync(UserQueryDto query)
    {
        // Build simple server-side predicate (no complex combining)
        Expression<Func<User, bool>>? predicate = null;
        bool hasSearch = !string.IsNullOrWhiteSpace(query.SearchTerm);
        bool hasRole = !string.IsNullOrWhiteSpace(query.Role);

        if (hasSearch && hasRole)
        {
            var searchTerm = query.SearchTerm!.ToLowerInvariant();
            predicate = u => 
                EF.Functions.Like(u.Username.ToLowerInvariant(), $"%{searchTerm}%") ||
                EF.Functions.Like(u.Email.ToLowerInvariant(), $"%{searchTerm}%") ||
                EF.Functions.Like(u.FullName.ToLowerInvariant(), $"%{searchTerm}%") &&
                u.Role == query.Role;
        }
        else if (hasSearch)
        {
            var searchTerm = query.SearchTerm!.ToLowerInvariant();
            predicate = u => 
                EF.Functions.Like(u.Username.ToLowerInvariant(), $"%{searchTerm}%") ||
                EF.Functions.Like(u.Email.ToLowerInvariant(), $"%{searchTerm}%") ||
                EF.Functions.Like(u.FullName.ToLowerInvariant(), $"%{searchTerm}%");
        }
        else if (hasRole)
        {
            predicate = u => u.Role == query.Role;
        }

        // Get total count (server-side)
        var totalCount = predicate != null 
            ? await _userRepository.CountUsersAsync(predicate) 
            : await _userRepository.CountUsersAsync();

        // Build server-side ordering
        Func<IQueryable<User>, IOrderedQueryable<User>>? orderBy = null;
        bool sortDesc = !(query.SortDescending.HasValue && !query.SortDescending.Value);
        string sortBy = (query.SortBy ?? "createdat").ToLower();

        orderBy = sortBy switch
        {
            "username" => sortDesc ? q => q.OrderByDescending(u => u.Username) : q => q.OrderBy(u => u.Username),
            "email" => sortDesc ? q => q.OrderByDescending(u => u.Email) : q => q.OrderBy(u => u.Email),
            "role" => sortDesc ? q => q.OrderByDescending(u => u.Role) : q => q.OrderBy(u => u.Role),
            "updatedat" => sortDesc ? q => q.OrderByDescending(u => u.UpdatedAt) : q => q.OrderBy(u => u.UpdatedAt),
            _ => sortDesc ? q => q.OrderByDescending(u => u.CreatedAt) : q => q.OrderBy(u => u.CreatedAt)
        };

        // Server-side query with filter/sort/pagination
        var skip = (query.PageNumber - 1) * query.PageSize;
        var items = await _userRepository.GetUsersAsync(predicate, orderBy, skip, query.PageSize);

        var dtos = items.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,
            Role = u.Role,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();

        return new PagedResultDto<AdminUserDto>
        {
            Items = dtos,
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

    public async Task<int> CreateUserAsync(CreateUserDto createUser)
    {
        var existedUser = await _userRepository.FindAsync(x=>x.Username==createUser.Username);
        if (existedUser.Any())
            throw new BadRequestException($"User with username {createUser.Username} already exists");
        var existedEmail = await _userRepository.FindAsync(x => x.Email == createUser.Email);
        if (existedEmail.Any())
            throw new BadRequestException($"User with email {createUser.Email} already exists");
        var passwordHash = HashPassword(createUser.Password);
        var newUser = await _userRepository.AddAsync(new User
        {
            Username = createUser.Username,
            Email = createUser.Email,
            PasswordHash = passwordHash,
            FullName = createUser.FullName,
            AvatarUrl = createUser.AvatarUrl,
            Role = createUser.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        return newUser.Id;
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
        var messageCounts = await _chatHistoryRepository.GetMessageCountByUserAsync(startDate, endDate) ?? new Dictionary<int, int>();
        
        // Get session counts by user
        var sessionCounts = await _userRepository.GetSessionCountByUserAsync(startDate, endDate) ?? new Dictionary<int, int>();

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
        Expression<Func<ChatSession, bool>>? predicate = null;
        bool hasUserId = query.UserId.HasValue;
        bool hasStartDate = query.StartDate.HasValue;
        bool hasEndDate = query.EndDate.HasValue;

        if (hasUserId && hasStartDate && hasEndDate)
        {
            predicate = s => s.UserId == query.UserId.Value &&
                s.CreatedAt >= query.StartDate.Value &&
                s.CreatedAt <= query.EndDate.Value;
        }
        else if (hasUserId && hasStartDate)
        {
            predicate = s => s.UserId == query.UserId.Value &&
                s.CreatedAt >= query.StartDate.Value;
        }
        else if (hasUserId && hasEndDate)
        {
            predicate = s => s.UserId == query.UserId.Value &&
                s.CreatedAt <= query.EndDate.Value;
        }
        else if (hasStartDate && hasEndDate)
        {
            predicate = s => s.CreatedAt >= query.StartDate.Value &&
                s.CreatedAt <= query.EndDate.Value;
        }
        else if (hasUserId)
        {
            predicate = s => s.UserId == query.UserId.Value;
        }
        else if (hasStartDate)
        {
            predicate = s => s.CreatedAt >= query.StartDate.Value;
        }
        else if (hasEndDate)
        {
            predicate = s => s.CreatedAt <= query.EndDate.Value;
        }

        var totalCount = predicate != null 
            ? await _chatSessionRepository.CountChatSessionsAsync(predicate) 
            : await _chatSessionRepository.CountChatSessionsAsync();

        Func<IQueryable<ChatSession>, IOrderedQueryable<ChatSession>>? orderBy = null;
        bool sortDesc = query.SortDescending;
        string sortBy = (query.SortBy ?? "createdat").ToLower();

        orderBy = sortBy switch
        {
            "sessionname" => sortDesc ? q => q.OrderByDescending(s => s.SessionName) : q => q.OrderBy(s => s.SessionName),
            "userid" => sortDesc ? q => q.OrderByDescending(s => s.UserId) : q => q.OrderBy(s => s.UserId),
            "updatedat" => sortDesc ? q => q.OrderByDescending(s => s.UpdatedAt) : q => q.OrderBy(s => s.UpdatedAt),
            _ => sortDesc ? q => q.OrderByDescending(s => s.CreatedAt) : q => q.OrderBy(s => s.CreatedAt)
        };

        var skip = (query.PageNumber - 1) * query.PageSize;
        var sessions = await _chatSessionRepository.GetChatSessionsAsync(predicate, orderBy, skip, query.PageSize, true, true);

        var items = sessions.Select(s => new AdminChatSessionDto
        {
            UserId = s.UserId,
            Username = s.User?.Username ?? "Unknown",
            SessionId = s.SessionId.ToString(),
            SessionName = s.SessionName,
            MessageCount = s.ChatHistories.Count,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();

        return new PagedResultDto<AdminChatSessionDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<AdminChatSessionDto?> GetChatSessionByIdAsync(Guid sessionId)
    {
        var session = await _chatSessionRepository.GetChatSessionWithUserAsync(sessionId);
        if (session == null) return null;

        return new AdminChatSessionDto
        {
            UserId = session.UserId,
            Username = session.User?.Username ?? "Unknown",
            SessionId = session.SessionId.ToString(),
            SessionName = session.SessionName,
            MessageCount = session.ChatHistories?.Count ?? 0,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<bool> DeleteChatSessionAsync(Guid sessionId)
    {
        var sessions = await _chatSessionRepository.FindAsync(x=>x.SessionId == sessionId);
        var session = sessions.FirstOrDefault();
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

    #region Person Summary

    /// <summary>
    /// Get person summary from Wikipedia: extract → LLM summarize → return formatted response
    /// </summary>
    public async Task<PersonSummaryResponseDto> GetPersonSummaryAsync(PersonSummaryRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName))
        {
            return new PersonSummaryResponseDto
            {
                Status = "error",
                Message = "EntityName is required"
            };
        }

        try
        {
            var language = string.IsNullOrWhiteSpace(request.Language) ? "vi" : request.Language.ToLower();
            _logger.LogInformation("Fetching person summary for {EntityName} (lang: {Language})", request.EntityName, language);

            // Step 1: Fetch Wikipedia extract (plaintext)
            var wikiData = await _wikipediaService.GetArticleFullContentAsync(request.EntityName, language);
            if (wikiData == null)
            {
                // Try search if exact match fails
                var searchResults = await _wikipediaService.SearchAsync(request.EntityName, language, 5);
                if (searchResults?.Count > 0)
                {
                    wikiData = await _wikipediaService.GetArticleFullContentAsync(searchResults[0].Title, language);
                }
            }

            if (wikiData == null)
            {
                return new PersonSummaryResponseDto
                {
                    Status = "error",
                    Message = $"Wikipedia article not found for '{request.EntityName}'. Try disambiguation search."
                };
            }

            var html = wikiData?.FullContent ?? string.Empty;
            if (string.IsNullOrWhiteSpace(html))
            {
                return new PersonSummaryResponseDto
                {
                    Status = "error",
                    Message = "No content found in Wikipedia article."
                };
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Flexible content extraction for desktop/mobile Wikipedia - using //*[@class='mw-parser-output']
            HtmlNode? contentDiv = doc.GetElementbyId("mw-content-text") 
                                ?? doc.GetElementbyId("content")
                                ?? doc.DocumentNode.SelectSingleNode("//div[@class='mw-parser-output']")
                                ?? doc.DocumentNode.SelectSingleNode("//section[@class='mw-parser-output']")
                                ?? doc.DocumentNode.SelectSingleNode("//div[@id='bodyContent']")
                                ?? doc.DocumentNode.SelectSingleNode("//main[@id='bodyContent']")
                                ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'mw-body-content')]");

            string usedSelector = contentDiv?.Id ?? contentDiv?.GetAttributeValue("class", "unknown");
            _logger.LogInformation("Found content div using selector: {Selector} (Title: {DocTitle})", usedSelector, doc.DocumentNode.SelectSingleNode("//title")?.InnerText);

            if (contentDiv == null)
            {
                return new PersonSummaryResponseDto
                {
                    Status = "error",
                    Message = "No content div found in Wikipedia HTML. Tried: mw-content-text, content, .mw-parser-output, .mw-body-content, #bodyContent."
                };
            }
            
            var paragraphs = contentDiv.SelectNodes("//p");
            if (paragraphs == null || paragraphs.Count == 0)
            {
                return new PersonSummaryResponseDto
                {
                    Status = "error",
                    Message = "No paragraphs found in content div."
                };
            }
            
            _logger.LogInformation("Extracted {ParaCount} paragraphs from Wikipedia content", paragraphs.Count);
            
            var sb = new StringBuilder();
            int paraCount = 0;
            foreach (var p in paragraphs) // Take more paras for better summary
            {
                var text = p.InnerText?.Trim();
                // if (!string.IsNullOrWhiteSpace(text) && text.Length > 20) // Skip very short paras
                // {
                    sb.AppendLine(text);
                    paraCount++;
                    // if (sb.Length > 50000) break; // Limit total length
                // }
            }
            
            _logger.LogInformation("Built summary from {ParaCount} paragraphs ({Length} chars)", paraCount, sb.Length);

            var rawSummary = sb.ToString();
            if (string.IsNullOrWhiteSpace(rawSummary))
            {
                return new PersonSummaryResponseDto
                {
                    Status = "error",
                    Message = "No content after parsing."
                };
            }

            // Chain all cleaning on rawSummary from HtmlAgilityPack (first 3 paragraphs)
            var summary = rawSummary;

            // Task-specific cleaning: remove citations, normalize
            summary = System.Text.RegularExpressions.Regex.Replace(summary, @"\[\d+\]|\[cần dẫn nguồn\]|\[citation needed\]", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
            summary = System.Text.RegularExpressions.Regex.Replace(summary, @"\s{2,}", " ");
            summary = System.Text.RegularExpressions.Regex.Replace(summary, @"\s*-\s*", "-"); // Normalize dates like ( 1906 - 03 - 01 )
            summary = System.Text.RegularExpressions.Regex.Replace(summary, @"[ \t\n\r\f\v]+\.{3}", "."); // Replace excessive ... with .

            // Remove duplicate title at start e.g. "Phạm Văn Đồng Phạm Văn Đồng" -> "Phạm Văn Đồng ..."
            var words = summary.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 4 && string.Equals(words[0], words[2], StringComparison.OrdinalIgnoreCase) && string.Equals(words[1], words[3], StringComparison.OrdinalIgnoreCase))
            {
                summary = string.Join(" ", words.Skip(2));
            }
            
            
            // No length limit - return full cleaned content

            if (string.IsNullOrWhiteSpace(summary))
            {
                return new PersonSummaryResponseDto
                {
                    Status = "error",
                    Message = "No content found in Wikipedia article."
                };
            }

            // Step 4: Build response
            var sourceUrl = $"https://{language}.wikipedia.org/wiki/{Uri.EscapeDataString(request.EntityName.Replace(" ", "_"))}";
            var data = new PersonSummaryDataDto
            {
                Name = wikiData.Title ?? request.EntityName,
                Summary = summary, // Full clean text (no HTML, no limit)
                SourceUrl = sourceUrl,
                ExtractedDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };

            // Auto update document if DocumentId provided
            if (request.DocumentId.HasValue)
            {
                var document = await _documentRepository.GetByIdAsync(request.DocumentId.Value);
    
                if (document != null)
                {
                    _logger.LogInformation("Updating existing document ID: {DocumentId}", request.DocumentId.Value);
                    
                    document.Description = data.Summary;
                    document.WikipediaUrl = data.SourceUrl;
                    
                    if (request.CategoryId.HasValue)
                    {
                        document.CategoryId = request.CategoryId.Value;
                    }
                    
                    document.UpdatedAt = DateTime.UtcNow;
                    
                    // Đảm bảo hàm UpdateAsync này thực hiện lệnh SQL UPDATE, không phải INSERT
                    await _documentRepository.UpdateAsync(document); 
                }
                else
                {
                    // Nếu không tìm thấy DocumentId, tuyệt đối không tự ý INSERT cái mới ở đây
                    _logger.LogWarning("Document ID {DocumentId} not found. Skip updating.", request.DocumentId.Value);
                    
                    // Bạn có thể trả về lỗi luôn để Frontend biết
                    return new PersonSummaryResponseDto { Status = "error", Message = "Document không tồn tại để cập nhật." };
                }
            }

            return new PersonSummaryResponseDto
            {
                Status = "success",
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating person summary for {EntityName}", request.EntityName);
            return new PersonSummaryResponseDto
            {
                Status = "error",
                Message = ex.Message
            };
        }
    }

    #endregion

    #region Document Management - Wikipedia Import

    #endregion

    #region Document Management - Wikipedia Import

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return sanitized.Replace(" ", "_");
    }

    #endregion

    public async Task<WikipediaGenerateNodeResponseDto> GenerateWikipediaNodeAsync(WikipediaGenerateNodeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new WikipediaGenerateNodeResponseDto
            {
                Success = false,
                Message = "Name is required"
            };
        }

        try
        {
            var language = string.IsNullOrWhiteSpace(request.Language) ? "vi" : request.Language.ToLower();
        
            // SỬ DỤNG CLASS MỚI TẠI ĐÂY
            WikipediaFullContentResponse? wikiData = null;

            if (request.Name.StartsWith("http"))
            {
                // Nếu hàm này trả về SummaryResponse, em cần map nó sang FullContentResponse 
                // hoặc tạo một hàm Fetch tương tự cho FullContent
                wikiData = await FetchFullWikipediaFromUrlAsync(request.Name, language);
            }
            else
            {
                // Gọi Service mới lấy Full Content
                wikiData = await _wikipediaService.GetArticleFullContentAsync(request.Name, language);
                
                if (wikiData == null)
                {
                    var searchResults = await _wikipediaService.SearchAsync(request.Name, language, 1);
                    if (searchResults != null && searchResults.Count > 0)
                    {
                        wikiData = await _wikipediaService.GetArticleFullContentAsync(searchResults[0].Title, language);
                    }
                }
            }

            if (wikiData == null) return new WikipediaGenerateNodeResponseDto { Success = false, Message = "Not found" };

            // Dùng hàm Build mới dành riêng cho FullContentResponse
            var content = BuildFullWikipediaContent(wikiData);
            
            // Logic ghi file debug và gọi RAG Service giữ nguyên...
            var filename = $"{SanitizeFileName(request.CustomTitle ?? wikiData.Title)}.md";
            var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            await File.WriteAllTextAsync(debugPath, content);

            var ragServiceResponse = await _ragService.GenerateNodeAsync(request.TargetPerson ?? wikiData.Title, filename, content);
            
            return new WikipediaGenerateNodeResponseDto {
                Success = true,
                Message = $"Queued job: {ragServiceResponse.JobId}",
                GraphRagJobId = ragServiceResponse.JobId,
                Data = new { WikipediaTitle = wikiData.Title }
            };
        }
        catch (Exception ex) { return new WikipediaGenerateNodeResponseDto { Success = false, Message = ex.Message }; }
    }

    private string BuildFullWikipediaContent(WikipediaFullContentResponse wikiData)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {wikiData.Title}");
        if (!string.IsNullOrWhiteSpace(wikiData.Description)) sb.AppendLine($"> {wikiData.Description}\n");

        // Ưu tiên FullContent vì đây là dữ liệu chính cho GraphRAG
        if (!string.IsNullOrWhiteSpace(wikiData.FullContent))
        {
            sb.AppendLine(wikiData.FullContent);
        }
        else
        {
            sb.AppendLine("## Summary");
            sb.AppendLine(wikiData.Extract);
        }

        sb.AppendLine("\n---");
        sb.AppendLine("## Metadata");
        sb.AppendLine($"- Source: Wikipedia ({wikiData.ContentUrls?.Desktop?.Page})");
        return sb.ToString();
    }

    private async Task<WikipediaFullContentResponse?> FetchFullWikipediaFromUrlAsync(string url, string language)
    {
        try
        {
            // 1. Phân tích URL để lấy Title
            // Ví dụ: https://vi.wikipedia.org/wiki/Nguyễn_Trãi
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            
            if (path.StartsWith("/wiki/"))
            {
                // Cắt bỏ phần "/wiki/" (6 ký tự)
                var title = path.Substring(6); 
                
                // Unescape để chuyển từ "Nguy%E1%BB%85n_Tr%C3%A3i" thành "Nguyễn Trãi"
                title = Uri.UnescapeDataString(title.Replace("_", " "));
                
                _logger.LogInformation("Extracted title for Full Content from URL: {Title}", title);

                // 2. Gọi Service lấy Full Content thay vì Summary
                return await _wikipediaService.GetArticleFullContentAsync(title, language);
            }
            
            _logger.LogWarning("URL format not supported for Wikipedia extraction: {Url}", url);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting full content title from URL: {Url}", url);
            return null;
        }
    }

    #region Wikipedia Chunking

    /// <summary>
    /// Start Wikipedia chunking: extract → temp file → queue RAG process
    /// </summary>
    public async Task<WikipediaChunkingResponseDto> StartWikipediaChunkingAsync(WikipediaChunkingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        var chunkSize = request.ChunkSize ?? 800;
        var chunkOverlap = request.ChunkOverlap ?? 150;
        if (chunkSize < 100 || chunkSize > 2000)
            throw new ArgumentException("chunkSize must be 100-2000");
        if (chunkOverlap > chunkSize * 0.25)
            throw new ArgumentException("chunkOverlap cannot exceed 25% of chunkSize");

        var language = DetectLanguage(request.Name) ?? request.Language ?? "en";
        if (language != "en" && language != "vi")
            language = "en";

        try
        {
            _logger.LogInformation("Starting Wikipedia chunking for {Name} (lang: {Lang}, chunk: {Size}/{Overlap})", request.Name, language, chunkSize, chunkOverlap);

            WikipediaFullContentResponse? wikiData = await _wikipediaService.GetArticleFullContentAsync(request.Name, language);
            if (wikiData == null)
            {
                var searchResults = await _wikipediaService.SearchAsync(request.Name, language, 5);
                if (searchResults?.Count > 0)
                {
                    wikiData = await _wikipediaService.GetArticleFullContentAsync(searchResults[0].Title, language);
                }
            }

            if (wikiData == null)
            {
                throw new KeyNotFoundException($"Wikipedia article not found for '{request.Name}' (lang: {language})");
            }

            var title = !string.IsNullOrWhiteSpace(request.CustomTitle) ? request.CustomTitle : wikiData.Title;
            var fileName = $"{SanitizeFileName(title)}.txt";

            var content = BuildWikipediaChunkContent(wikiData);
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Gọi RAG Service
            var uploadResponse = await _ragService.UploadDocumentAsync(memoryStream, fileName, chunkSize, chunkOverlap);

            // FIX LỖI BUILD: 
            // Vì DocumentUploadResponse không có BatchId, chúng ta dùng batchId tự tạo.
            // Vì không có thuộc tính Success, chúng ta mặc định là true nếu không ném Exception.
            // Lấy BatchId từ server trả về thay vì tạo ngẫu nhiên
            var internalBatchId = uploadResponse.BatchId ?? Guid.NewGuid().ToString();
            var jobsList = new List<object>();

            if (uploadResponse.Jobs != null && uploadResponse.Jobs.Any())
            {
                foreach (var job in uploadResponse.Jobs)
                {
                    var ragJobId = job.JobId ?? $"pending_{internalBatchId}";
                    jobsList.Add(new {
                        // Sử dụng toán tử ?? để tránh trả về null cho client
                        job_id = ragJobId,
                        file_name = job.FileName, 
                        status = job.Status ?? "queued"
                    });

                    // Also enqueue the same job to the Graph pipeline so the
                    // Graph-RAG worker can build the knowledge graph in parallel.
                    if (_documentQueueService != null && ragJobId != null && !ragJobId.StartsWith("pending_"))
                    {
                        var wikiFileUrl = $"https://{language}.wikipedia.org/api/rest_v1/page/mobile-html/{Uri.EscapeDataString(title.Replace(" ", "_"))}";
                        var graphJob = new DocumentProcessingJobDto
                        {
                            JobId = ragJobId,
                            DocumentId = string.Empty,
                            UserId = "admin",
                            FileName = Path.GetFileNameWithoutExtension(fileName) + ".html",
                            FileUrl = wikiFileUrl,
                            FileType = "html",
                            RetryCount = 0,
                            CreatedAt = DateTime.UtcNow
                        };
                        try
                        {
                            await _documentQueueService.EnqueueGraphTaskAsync(graphJob);
                            _logger.LogInformation(
                                "Enqueued Wikipedia article '{Title}' (job {JobId}) to Graph pipeline",
                                title, ragJobId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "Failed to enqueue Wikipedia job {JobId} to Graph pipeline", ragJobId);
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("RAG Service returned empty jobs for {FileName}.", fileName);
                jobsList.Add(new {
                    job_id = $"pending_{internalBatchId}", 
                    file_name = fileName,
                    status = "processing_hidden"
                });
            }
            
            var wikiUrl = wikiData.ContentUrls?.Desktop?.Page ?? $"https://{language}.wikipedia.org/wiki/{Uri.EscapeDataString(title.Replace(" ", "_"))}";

            return new WikipediaChunkingResponseDto
            {
                success = true,
                message = "Document imported successfully from Wikipedia",
                batch_id = internalBatchId,
                jobs = jobsList.ToArray(),
                wikipediaTitle = wikiData.Title ?? "",
                wikipediaUrl = wikiUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Wikipedia chunking for {Name}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Get Wikipedia chunking job status
    /// </summary>
    public async Task<WikipediaJobStatusResponseDto?> GetWikipediaJobStatusAsync(string jobId)
    {
        try
        {
            var status = await _ragService.GetJobStatusAsync(jobId);
            if (status == null)
                return null;

            // Map RAG status to Wikipedia DTO
            // Lưu ý: Chuyển các thuộc tính từ snake_case sang PascalCase (S, D, F, P, M, E...)
            return new WikipediaJobStatusResponseDto
            {
                job_id = jobId,
                status = status.Status?.ToLower() ?? "unknown",
                document_id = status.DocumentId,
                file_name = status.FileName ?? "wikipedia_doc.txt",
                progress = TryParseProgress(status.Progress, status.Status),
                message = status.Message ?? "Processing...",
                error = status.Error,
                timing = new {
                    start = status.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.UtcNow.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss"),
                    end = status.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
                }
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status {JobId}", jobId);
            throw;
        }
    }
    private string DetectLanguage(string nameOrUrl)
    {
        if (nameOrUrl.Contains("vi.wikipedia.org")) return "vi";
        if (nameOrUrl.Contains("en.wikipedia.org")) return "en";
        return null;
    }

    private string BuildWikipediaChunkContent(WikipediaFullContentResponse wikiData)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {wikiData.Title}");
        if (!string.IsNullOrWhiteSpace(wikiData.Description)) sb.AppendLine(wikiData.Description);
        if (!string.IsNullOrWhiteSpace(wikiData.FullContent)) sb.AppendLine(wikiData.FullContent);
        else if (!string.IsNullOrWhiteSpace(wikiData.Extract)) sb.AppendLine(wikiData.Extract);
        sb.AppendLine($"\nSource: {wikiData.ContentUrls?.Desktop?.Page}");
        return sb.ToString();
    }

    #endregion

    private int TryParseProgress(object? progressObj, string? status)
    {
        int progressValue = 0;

        // 1. Kiểm tra nếu nó là kiểu số (int, long, double...)
        if (progressObj is int i) progressValue = i;
        else if (progressObj is long l) progressValue = (int)l;
        else if (progressObj is double d) progressValue = (int)d;
        else if (progressObj is System.Text.Json.JsonElement element)
        {
            // Nếu là JsonElement (do dùng object trong DTO), ta trích xuất giá trị số
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
                element.TryGetInt32(out progressValue);
        }

        // 2. Fallback dựa trên Status nếu progress vẫn bằng 0
        if (progressValue == 0 && status?.ToLower() == "completed")
        {
            return 100;
        }

        return progressValue;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

}




