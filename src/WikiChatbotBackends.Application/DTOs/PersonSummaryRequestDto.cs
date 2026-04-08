using System.ComponentModel.DataAnnotations;

namespace WikiChatbotBackends.Application.DTOs;

public class PersonSummaryRequestDto
{
    [Required]
    public string EntityName { get; set; } = string.Empty;

    public string Language { get; set; } = "vi";

    public bool IsAutoSave { get; set; } = false;
}

