namespace WikiChatbotBackends.Application.DTOs;

public class ChatHistoryDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateChatHistoryDto
{
    public int SessionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class UpdateChatHistoryDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class ChatSessionDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateChatSessionDto
{
    public string SessionId { get; set; } = string.Empty; // GUID
    public string SessionName { get; set; } = string.Empty;
}

public class UpdateChatSessionDto
{
    public string SessionName { get; set; } = string.Empty;
}

public class SessionSummaryDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}
