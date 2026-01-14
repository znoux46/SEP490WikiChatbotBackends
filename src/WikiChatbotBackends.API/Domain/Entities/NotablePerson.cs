namespace WikiChatbotBackends.API.Domain.Entities;

public class NotablePerson
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<PersonAward> PersonAwards { get; set; } = new List<PersonAward>();
    public ICollection<PersonOrganization> PersonOrganizations { get; set; } = new List<PersonOrganization>();
    public ICollection<PersonTag> PersonTags { get; set; } = new List<PersonTag>();
}
