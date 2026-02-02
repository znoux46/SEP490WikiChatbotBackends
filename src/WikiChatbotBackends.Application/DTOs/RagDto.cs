using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WikiChatbotBackends.Application.DTOs
{
    // ============================================================================
    // CHAT REQUEST/RESPONSE
    // ============================================================================
    
    public class ChatRequest
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("document_ids")]
        public List<string>? DocumentIds { get; set; }

        [JsonPropertyName("verbose")]
        [DefaultValue(false)]
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
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("job_id")] // Phải map từ snake_case
        public string? JobId { get; set; }

        [JsonPropertyName("document_id")]
        public string? DocumentId { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class DocumentUploadResponse
    {
        [JsonPropertyName("total_files")] // Map từ total_files trong MultiFileUploadResponse
        public int TotalFiles { get; set; }

        [JsonPropertyName("results")]
        public List<FileUploadResult> Results { get; set; } = new();
    }

    // ============================================================================
    // DOCUMENT INFO
    // ============================================================================
    
    public class DocumentInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("file_path")] // Quan trọng: FastAPI trả về file_path
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("file_name")] // Quan trọng: FastAPI trả về file_name
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("source_type")]
        public string SourceType { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("chunk_count")] // Map chính xác count từ FastAPI
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
