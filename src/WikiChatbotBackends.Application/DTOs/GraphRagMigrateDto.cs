using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs;

public class GraphRagMigrateDto
{
    [JsonPropertyName("pg_dsn")]
    public string PgDsn { get; set; } = string.Empty;
    
    [JsonPropertyName("table_name")]
    public string TableName { get; set; } = "persons";
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;
}

