namespace WikiChatbotBackends.API.Domain.Entities;

public class Award
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<PersonAward> PersonAwards { get; set; } = new List<PersonAward>();
}
