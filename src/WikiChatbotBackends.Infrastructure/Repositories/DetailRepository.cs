using Microsoft.EntityFrameworkCore;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;
using System;
using System.Linq.Expressions;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class DetailRepository : Repository<Detail>, IDetailRepository
{
    public DetailRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Detail?> GetByIdWithCategoryAsync(Guid id)
    {
        return await _dbSet.Include(d => d.Category).FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Detail>> GetByCategoryIdWithCategoryAsync(Guid categoryId)
    {
        return await _dbSet.Where(d => d.CategoryId == categoryId)
            .Include(d => d.Category)
            .OrderBy(d => d.Title)
            .ToListAsync();
    }

    public async Task<Detail?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<bool> ExistsAsync(Expression<Func<Detail, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }
}
