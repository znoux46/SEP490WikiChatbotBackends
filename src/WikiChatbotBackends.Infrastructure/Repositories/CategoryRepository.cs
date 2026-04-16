using Microsoft.EntityFrameworkCore;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _dbSet.AnyAsync(c => c.Name == name);
    }

    public async Task<bool> ExistsByIdAsync(Guid id)
    {
        return await _dbSet.AnyAsync(c => c.Id == id);
    }

    public async Task<List<Category>> GetAllWithDetailsAsync()
    {
        return await _dbSet.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<List<Category>> GetForLandingAsync(int limit)
    {
        return await _dbSet.OrderByDescending(c => c.CreatedAt).Take(limit).ToListAsync();
    }
}