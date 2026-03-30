using System.ComponentModel.DataAnnotations;

namespace WikiChatbotBackends.Application.DTOs;

public class GenerateNodeRequestDto
{
    /// <summary>
    /// Target person/name for extraction (defaults to Filename without extension)
    /// </summary>
    [StringLength(200)]
    public string? TargetPerson { get; set; }

    /// <summary>
    /// Optional filename (used for default target_person = filename w/o ext if not provided)
    /// </summary>
    public string? DocumentId { get; set; } // Optional existing doc ID

    public string? Filename { get; set; } = string.Empty;
}

