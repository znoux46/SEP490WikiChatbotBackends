namespace WikiChatbotBackends.Domain.Entities;

public class ChatHistory
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ChatSession Session { get; set; } = null!;
}