using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class WikipediaJobStatusResponseDto
{
    [JsonPropertyName("job_id")]
    public string job_id { get; set; } = string.Empty;

    public string status { get; set; } = string.Empty;

    [JsonPropertyName("document_id")]
    public string? document_id { get; set; }

    [JsonPropertyName("file_name")]
    public string file_name { get; set; } = string.Empty;

    public int progress { get; set; }

    public string message { get; set; } = string.Empty;

    public string? error { get; set; }

    public object timing { get; set; } = new { start = "", end = "" };
}

