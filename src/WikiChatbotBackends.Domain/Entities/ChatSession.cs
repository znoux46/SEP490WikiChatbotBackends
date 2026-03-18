namespace WikiChatbotBackends.Domain.Entities;

public class ChatSession
{
    public Guid SessionId { get; set; }
    public int UserId { get; set; }
    public string SessionName { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ChatHistory> ChatHistories { get; set; } = new List<ChatHistory>();
}