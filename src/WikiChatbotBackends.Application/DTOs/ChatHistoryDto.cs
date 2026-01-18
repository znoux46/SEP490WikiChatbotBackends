namespace WikiChatbotBackends.Application.DTOs;

public class ChatHistoryDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateChatHistoryDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class UpdateChatHistoryDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class SessionSummaryDto
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}
