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
    // Thêm AsNoTracking để lấy dữ liệu thực tế từ DB, không dùng bản cache cũ
    return await _context.Documents
        .AsNoTracking() 
        .FirstOrDefaultAsync(d => d.Id == id);
}
}
