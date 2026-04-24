using Microsoft.EntityFrameworkCore;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Document?> GetByIdWithCategoryAsync(Guid id)
    {
        return await _dbSet.Include(d => d.Category).FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
    }

    public async Task<Document?> GetByTaskIdAsync(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return null;

        return await _dbSet
            .FromSqlInterpolated($@"
                SELECT *
                FROM documents
                WHERE is_deleted = false
                  AND metadata ->> 'task_id' = {taskId}
                LIMIT 1")
            .FirstOrDefaultAsync();
    }

    public async Task<Document?> GetByCategoryAndWikipediaUrlAsync(Guid categoryId, string wikipediaUrl)
    {
        if (string.IsNullOrWhiteSpace(wikipediaUrl))
            return null;

        return await _dbSet
            .Where(d => d.CategoryId == categoryId && !d.IsDeleted && d.WikipediaUrl == wikipediaUrl)
            .OrderByDescending(d => d.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Document>> GetAllWithCategoryAsync()
    {
        return await _dbSet
            .Where(d => !d.IsDeleted)
            .Include(d => d.Category)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Document>> GetByCategoryIdWithCategoryAsync(Guid categoryId)
    {
        return await _dbSet
            .Where(d => d.CategoryId == categoryId && !d.IsDeleted)
            .Include(d => d.Category)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
