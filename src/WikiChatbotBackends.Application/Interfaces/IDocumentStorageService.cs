using WikiChatbotBackends.Application.DTOs;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<UploadedDocumentResultDto> UploadAsync(
        DocumentUploadItemDto file,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string publicId,
        string resourceType = "raw",
        CancellationToken cancellationToken = default);
}
