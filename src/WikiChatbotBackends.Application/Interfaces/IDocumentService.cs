using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDocumentService
{
    Task<List<DocumentDto>> GetAllAsync(Guid? categoryId = null);
    Task<DocumentDto?> GetByIdAsync(Guid id);
    Task<List<DocumentDto>> UploadAsync(Guid categoryId, string userId, List<DocumentUploadItemDto> files, CancellationToken cancellationToken = default);
    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentPipelineWebhookResultDto> UpdatePipelineStatusAsync(DocumentPipelineWebhookDto dto, CancellationToken cancellationToken = default);
}
