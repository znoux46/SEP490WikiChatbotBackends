namespace WikiChatbotBackends.API.Domain.Entities;

public class PersonTag
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public int TagId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public NotablePerson Person { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
