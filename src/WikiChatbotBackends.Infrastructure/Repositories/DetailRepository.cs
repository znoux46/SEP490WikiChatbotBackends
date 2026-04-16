using Microsoft.EntityFrameworkCore;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;
using System;
using System.Linq.Expressions;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class DetailRepository : Repository<Document>, IDetailRepository
{
    public DetailRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Document?> GetByIdWithCategoryAsync(Guid id)
    {
        return await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Document>> GetByCategoryIdWithCategoryAsync(Guid categoryId)
    {
        return await _context.Documents.Where(d => d.CategoryId == categoryId)
            .OrderBy(d => d.FileName)
            .ToListAsync();
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<bool> ExistsAsync(Expression<Func<Document, bool>> predicate)
    {
        return await _context.Documents.AnyAsync(predicate);
    }
}
