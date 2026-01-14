namespace WikiChatbotBackends.API.Application.DTOs;

public class NotablePersonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public List<AwardDto> Awards { get; set; } = new();
    public List<OrganizationDto> Organizations { get; set; } = new();
    public List<TagDto> Tags { get; set; } = new();
}

public class CreateNotablePersonDto
{
    public string Name { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public List<int> TagIds { get; set; } = new();
}

public class UpdateNotablePersonDto
{
    public string Name { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }
    public string Nationality { get; set; } = string.Empty;
}
