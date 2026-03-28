using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class GenerateNodeResponseDto
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing result or error
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Job ID from model server if applicable
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JobId { get; set; }

    /// <summary>
    /// Raw response from model server /upload
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}
