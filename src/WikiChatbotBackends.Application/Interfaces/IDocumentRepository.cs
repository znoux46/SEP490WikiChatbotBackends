using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetByIdAsync(Guid id);
}
