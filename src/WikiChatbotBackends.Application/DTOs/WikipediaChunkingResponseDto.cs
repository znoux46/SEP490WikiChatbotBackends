using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class WikipediaChunkingResponseDto
{
    [JsonPropertyName("success")]
    public bool success { get; set; }

    public string message { get; set; } = string.Empty;

    [JsonPropertyName("batch_id")]
    public string batch_id { get; set; } = string.Empty;

    public object[] jobs { get; set; } = Array.Empty<object>();

    [JsonPropertyName("wikipediaTitle")]
    public string wikipediaTitle { get; set; } = string.Empty;

    [JsonPropertyName("wikipediaUrl")]
    public string wikipediaUrl { get; set; } = string.Empty;
}

