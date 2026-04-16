using System.ComponentModel.DataAnnotations;

namespace WikiChatbotBackends.Application.DTOs;

public class WikipediaGenerateNodeRequestDto
{
    /// <summary>
    /// Name of historical figure to generate Wikipedia content + GraphRAG nodes
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom title for filename/content
    /// </summary>
    public string? CustomTitle { get; set; }

    /// <summary>
    /// Language (en/vi, default en)
    /// </summary>
    public string? Language { get; set; } = "en";

    /// <summary>
    /// Chunk size for RAG processing (default 800)
    /// </summary>
    public int ChunkSize { get; set; } = 800;

    /// <summary>
    /// Chunk overlap for RAG processing (default 150)
    /// </summary>
    public int ChunkOverlap { get; set; } = 150;

    /// <summary>
    /// Target person for GraphRAG extraction (defaults to Name)
    /// </summary>
    [StringLength(200)]
    public string? TargetPerson { get; set; }
}

