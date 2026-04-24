using System.ComponentModel.DataAnnotations.Schema;

namespace WikiChatbotBackends.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public string? Status { get; set; }
    public int? FileSize { get; set; }
    public string? ContentHash { get; set; }
    public string? Metadata { get; set; }
    public string? Content { get; set; }
    public string? Description { get; set; }
    public string? WikipediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }
}
