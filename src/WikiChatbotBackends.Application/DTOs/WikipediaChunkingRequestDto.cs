using System.ComponentModel.DataAnnotations;

namespace WikiChatbotBackends.Application.DTOs;

public class WikipediaChunkingRequestDto
{
    /// <summary>
    /// Name of person or Wikipedia URL
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Custom title for document (default: Wikipedia title)
    /// </summary>
    public string? CustomTitle { get; set; }

    /// <summary>
    /// Chunk size (default 800, min 100 max 2000)
    /// </summary>
    public int? ChunkSize { get; set; } = 800;

    /// <summary>
    /// Chunk overlap (default 150, max 25% chunkSize)
    /// </summary>
    public int? ChunkOverlap { get; set; } = 150;

    /// <summary>
    /// Language vi/en (auto-detect from URL)
    /// </summary>
    public string? Language { get; set; } = "en";
}

