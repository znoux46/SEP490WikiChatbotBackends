using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetByIdWithCategoryAsync(Guid id);
    Task<Document?> GetByIdAsync(Guid id);
    Task<Document?> GetByTaskIdAsync(string taskId);
    Task<Document?> GetByCategoryAndWikipediaUrlAsync(Guid categoryId, string wikipediaUrl);
    Task<List<Document>> GetAllWithCategoryAsync();
    Task<List<Document>> GetByCategoryIdWithCategoryAsync(Guid categoryId);
}
