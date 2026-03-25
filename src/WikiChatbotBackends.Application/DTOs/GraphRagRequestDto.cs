using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class GraphRagRequestDto
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;
}


