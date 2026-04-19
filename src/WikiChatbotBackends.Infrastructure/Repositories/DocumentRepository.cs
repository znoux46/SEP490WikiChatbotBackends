using Microsoft.EntityFrameworkCore;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;
using WikiChatbotBackends.Infrastructure.Repositories;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }
}
