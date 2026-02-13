using Microsoft.EntityFrameworkCore;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;

namespace WikiChatbotBackends.Application.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    #region User Management

    public async Task<PagedResultDto<AdminUserDto>> GetAllUsersAsync(UserQueryDto query)
    {
        var queryable = _context.Users.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(u => 
                u.Username.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                u.FullName.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            queryable = queryable.Where(u => u.Role == query.Role);
        }

        // Apply sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "username" => query.SortDescending 
                ? queryable.OrderByDescending(u => u.Username) 
                : queryable.OrderBy(u => u.Username),
            "email" => query.SortDescending 
                ? queryable.OrderByDescending(u => u.Email) 
                : queryable.OrderBy(u => u.Email),
            "role" => query.SortDescending 
                ? queryable.OrderByDescending(u => u.Role) 
                : queryable.OrderBy(u => u.Role),
            "updatedat" => query.SortDescending 
                ? queryable.OrderByDescending(u => u.UpdatedAt) 
                : queryable.OrderBy(u => u.UpdatedAt),
            _ => queryable.OrderByDescending(u => u.CreatedAt)
        };

        var totalCount = await queryable.CountAsync();
        var items = await queryable
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
            .ToListAsync();

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
        var user = await _context.Users.FindAsync(userId);
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
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            // Check if email is already used by another user
            var existingEmail = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.Id != userId);
            if (existingEmail)
                throw new InvalidOperationException("Email is already in use");

            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.Role))
            user.Role = dto.Role;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

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
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AdminUserDto> UpdateUserRoleAsync(int userId, string role)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        // Validate role
        var validRoles = new[] { "User", "Admin", "Moderator" };
        if (!validRoles.Contains(role))
            throw new InvalidOperationException($"Invalid role. Valid roles are: {string.Join(", ", validRoles)}");

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

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
        var totalUsers = await _context.Users.CountAsync();
        var totalChatSessions = await _context.ChatSessions.CountAsync();
        var totalChatMessages = await _context.ChatHistories.CountAsync();
        var totalAdmins = await _context.Users.CountAsync(u => u.Role == "Admin");
        var totalRegularUsers = await _context.Users.CountAsync(u => u.Role == "User");

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

            var newUsers = await _context.Users
                .CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate);

            var newChatSessions = await _context.ChatSessions
                .CountAsync(s => s.CreatedAt >= date && s.CreatedAt < nextDate);

            var newMessages = await _context.ChatHistories
                .CountAsync(h => h.CreatedAt >= date && h.CreatedAt < nextDate);

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

    #region Chat Session Management

    public async Task<PagedResultDto<AdminChatSessionDto>> GetAllChatSessionsAsync(ChatSessionQueryDto query)
    {
        var queryable = _context.ChatSessions
            .Include(s => s.User)
            .AsQueryable();

        // Apply filters
        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(s => s.UserId == query.UserId.Value);
        }

        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(s => s.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(s => s.CreatedAt <= query.EndDate.Value);
        }

        // Apply sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "sessionname" => query.SortDescending 
                ? queryable.OrderByDescending(s => s.SessionName) 
                : queryable.OrderBy(s => s.SessionName),
            "userid" => query.SortDescending 
                ? queryable.OrderByDescending(s => s.UserId) 
                : queryable.OrderBy(s => s.UserId),
            "updatedat" => query.SortDescending 
                ? queryable.OrderByDescending(s => s.UpdatedAt) 
                : queryable.OrderBy(s => s.UpdatedAt),
            _ => queryable.OrderByDescending(s => s.CreatedAt)
        };

        var totalCount = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new AdminChatSessionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Username,
                SessionId = s.SessionId,
                SessionName = s.SessionName,
                MessageCount = s.ChatHistories.Count,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

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
        var session = await _context.ChatSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null) return null;

        return new AdminChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Username = session.User.Username,
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            MessageCount = session.ChatHistories.Count,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<bool> DeleteChatSessionAsync(int sessionId)
    {
        var session = await _context.ChatSessions.FindAsync(sessionId);
        if (session == null)
            throw new KeyNotFoundException($"Chat session with id {sessionId} not found");

        _context.ChatSessions.Remove(session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAllUserChatSessionsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id {userId} not found");

        var sessions = await _context.ChatSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _context.ChatSessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion
}

