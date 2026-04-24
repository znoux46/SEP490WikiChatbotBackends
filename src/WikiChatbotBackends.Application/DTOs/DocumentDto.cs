namespace WikiChatbotBackends.Application.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Format { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long Bytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateDocumentDto
{
    public string FileName { get; set; } = string.Empty;
}

public class UploadDocumentRequestDto
{
    public Guid CategoryId { get; set; }
}

public class DocumentUploadItemDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long Length { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Func<Stream> OpenReadStream { get; set; } = null!;
}

public class UploadedDocumentResultDto
{
    public string PublicId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "raw";
    public string? Format { get; set; }
    public long Bytes { get; set; }
}

public class WikipediaHtmlResultDto
{
    public string HtmlContent { get; set; } = string.Empty;
    public string WikipediaUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

public class SearchWikipediaDocumentRequestDto
{
    public string Keyword { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
    public int Limit { get; set; } = 5;
}

public class WikipediaDocumentSearchItemDto
{
    public int PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Extract { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string PageUrl { get; set; } = string.Empty;
}

public class WikipediaUpdateDto
{
    public Guid CategoryId { get; set; }
    public string PageTitle { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
}
