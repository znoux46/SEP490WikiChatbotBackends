namespace WikiChatbotBackends.Application.DTOs;

public class DocumentPipelineWebhookDto
{
    public Guid? DocumentId { get; set; }
    public string? TaskId { get; set; }
    public string Pipeline { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? FileName { get; set; }
}

public class DocumentPipelineWebhookResultDto
{
    public Guid DocumentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RagStatus { get; set; } = string.Empty;
    public string GraphStatus { get; set; } = string.Empty;
    public string? TaskId { get; set; }
}
