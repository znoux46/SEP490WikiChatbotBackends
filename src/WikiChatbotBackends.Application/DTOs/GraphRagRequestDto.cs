using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class GraphRagRequestDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("target_person")]
    public string TargetPerson { get; set; } = string.Empty;
    
    [JsonPropertyName("source_type")]
    public string SourceType { get; set; } = "wiki";
    
    [JsonPropertyName("config")]
    public Dictionary<string, object>? Config { get; set; }
}

