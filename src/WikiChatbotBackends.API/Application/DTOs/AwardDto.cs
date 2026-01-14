namespace WikiChatbotBackends.API.Application.DTOs;

public class AwardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateAwardDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateAwardDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AssignAwardDto
{
    public int PersonId { get; set; }
    public int AwardId { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

public class RemoveAwardDto
{
    public int PersonId { get; set; }
    public int AwardId { get; set; }
}
