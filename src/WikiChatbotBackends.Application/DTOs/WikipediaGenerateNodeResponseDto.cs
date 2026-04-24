using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class WikipediaGenerateNodeResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GraphRagJobId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RAGDocumentId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

