namespace WikiChatbotBackends.Application.DTOs;

public class DocumentProcessingJobDto
{
    public string JobId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
