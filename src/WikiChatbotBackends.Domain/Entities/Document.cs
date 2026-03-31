using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WikiChatbotBackends.Domain.Entities;

[Table("documents")]
public class Document
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;
    [Column("source_type")]
    public string SourceType { get; set; } = string.Empty;
    [Column("status")]
    public string Status { get; set; } = string.Empty;
    [Column("file_size")]
    public int FileSize { get; set; }
    [Column("content_hash")]
    public string ContentHash { get; set; } = string.Empty;
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    [Column("category_id")]
    public Guid? CategoryId { get; set; }
    [Column("description")]
    public string? Description { get; set; }
}
