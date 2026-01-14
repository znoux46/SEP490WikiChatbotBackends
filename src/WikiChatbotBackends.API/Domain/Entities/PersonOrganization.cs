namespace WikiChatbotBackends.API.Domain.Entities;

public class PersonOrganization
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public int OrganizationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public NotablePerson Person { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
