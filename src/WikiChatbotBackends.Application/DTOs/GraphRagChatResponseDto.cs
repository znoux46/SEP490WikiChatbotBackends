using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class GraphRagChatResponseDto
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Answer from model /chat
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Raw response from model server
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}
