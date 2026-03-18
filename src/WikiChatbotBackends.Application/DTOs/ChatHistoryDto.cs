namespace WikiChatbotBackends.Application.DTOs;

public class ChatHistoryDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateChatHistoryDto
{
    public Guid SessionId { get; set; }
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
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateChatSessionDto
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
}

public class UpdateChatSessionDto
{
    public string SessionName { get; set; } = string.Empty;
}

public class SessionSummaryDto
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}
