namespace WikiChatbotBackends.API.Domain.Entities;

public class PersonAward
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public int AwardId { get; set; }
    public DateTime AwardedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public NotablePerson Person { get; set; } = null!;
    public Award Award { get; set; } = null!;
}
