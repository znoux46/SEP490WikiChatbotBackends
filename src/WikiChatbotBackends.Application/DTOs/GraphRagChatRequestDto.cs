using System.ComponentModel.DataAnnotations;

namespace WikiChatbotBackends.Application.DTOs;

public class GraphRagChatRequestDto
{
    /// <summary>
    /// User question
    /// </summary>
    [Required]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Session ID for history/context (similar to /api/Question)
    /// </summary>
    public Guid SessionId { get; set; }
}
