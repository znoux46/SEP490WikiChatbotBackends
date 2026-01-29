using System;
using System.Collections.Generic;

namespace WikiChatbotBackends.Application.DTOs
{
    // ============================================================================
    // CHAT REQUEST/RESPONSE
    // ============================================================================
    
    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
        public List<string>? DocumentIds { get; set; }
        public bool Verbose { get; set; } = false;
    }

    public class ChatResponse
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // ============================================================================
    // SEARCH REQUEST/RESPONSE
    // ============================================================================
    
    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int TopK { get; set; } = 10;
        public List<string>? DocumentIds { get; set; }
        public string SearchType { get; set; } = "hybrid"; // bm25, semantic, hybrid
        public float Bm25Weight { get; set; } = 0.6f;
        public float SemanticWeight { get; set; } = 0.4f;
    }

    public class SearchResult
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public float Score { get; set; }
        public string? H1 { get; set; }
        public string? H2 { get; set; }
        public string? H3 { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class SearchResponse
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchResult> Results { get; set; } = new();
        public int Total { get; set; }
        public string SearchType { get; set; } = string.Empty;
    }

    // ============================================================================
    // DOCUMENT UPLOAD REQUEST/RESPONSE
    // ============================================================================
    
    public class DocumentUploadRequest
    {
        public int ChunkSize { get; set; } = 800;
        public int ChunkOverlap { get; set; } = 150;
    }

    public class FileUploadResult
    {
        public string Filename { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // processing, duplicate, failed
        public string? JobId { get; set; }
        public string? DocumentId { get; set; }
        public string? Message { get; set; }
    }

    public class DocumentUploadResponse
    {
        public int TotalFiles { get; set; }
        public List<FileUploadResult> Results { get; set; } = new();
    }

    // ============================================================================
    // DOCUMENT INFO
    // ============================================================================
    
    public class DocumentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ChunkCount { get; set; }
    }

    // ============================================================================
    // JOB STATUS
    // ============================================================================
    
    public class JobStatusResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, processing, completed, failed
        public string? Message { get; set; }
        public string? DocumentId { get; set; }
        public Dictionary<string, object>? Progress { get; set; }
    }
}
