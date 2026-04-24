namespace WikiChatbotBackends.Application.DTOs;

public class PersonSummaryDataDto
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty; // Full clean text (no truncation, no HTML)
    public string SourceUrl { get; set; } = string.Empty;
    public string ExtractedDate { get; set; } = string.Empty;
}

