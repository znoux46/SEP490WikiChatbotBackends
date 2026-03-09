namespace WikiChatbotBackends.Application.DTOs;

public class AdminUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateUserRoleDto
{
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalChatSessions { get; set; }
    public int TotalChatMessages { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalRegularUsers { get; set; }
    public List<DailyStatsDto>? DailyStats { get; set; }
}

public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int NewChatSessions { get; set; }
    public int NewMessages { get; set; }
}

public class UserQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class AdminChatSessionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ChatSessionQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

// Time-based statistics grouped by day/week/month
public enum TimeGrouping
{
    Day,
    Week,
    Month
}

public class TimeSeriesStatsDto
{
    public DateTime Period { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int SessionCount { get; set; }
    public int UserCount { get; set; }
}

public class UserGrowthDto
{
    public DateTime Period { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int NewUsers { get; set; }
    public int TotalUsers { get; set; }
    public double GrowthRate { get; set; }
}

public class UserGrowthStatsDto
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int CumulativeUsers { get; set; }
}

public class ActiveUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalSessions { get; set; }
    public DateTime LastActiveAt { get; set; }
}

public class TopActiveUsersQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "TotalQuestions"; // TotalQuestions, TotalDocuments, TotalSessions
    public bool SortDescending { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

