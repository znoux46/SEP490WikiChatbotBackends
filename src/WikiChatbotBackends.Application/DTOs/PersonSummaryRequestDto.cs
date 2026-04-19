using System.ComponentModel.DataAnnotations;

namespace WikiChatbotBackends.Application.DTOs;

public class PersonSummaryRequestDto
{
    [Required]
    public string EntityName { get; set; } = string.Empty;

    public string Language { get; set; } = "vi";

    public bool IsAutoSave { get; set; } = false;

    /// <summary>
    /// Optional Document ID to auto-update description and wikipedia_url after generating summary
    /// </summary>
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Optional Category ID to set on document after generating summary
    /// </summary>
    public Guid? CategoryId { get; set; }
}

