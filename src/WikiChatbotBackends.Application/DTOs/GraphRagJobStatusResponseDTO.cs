using System.Text.Json.Serialization;

public class GraphRagJobStatusResponseDTO
{
    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("target_person")]
    public string? TargetPerson { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("nodes_created")]
    public int NodesCreated { get; set; }

    [JsonPropertyName("relationships_created")]
    public int RelationshipsCreated { get; set; }

    [JsonPropertyName("profile")]
    public object? Profile { get; set; } // Dùng object vì profile có thể là null hoặc json phức tạp

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Các trường phụ trợ khác nếu em cần dùng
    [JsonPropertyName("source_type")]
    public string? SourceType { get; set; }
}