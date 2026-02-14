using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class ChatHistoryRepository : Repository<ChatHistory>, IChatHistoryRepository
{
    public ChatHistoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ChatHistory>> GetChatHistoriesAsync(
        Expression<Func<ChatHistory, bool>>? predicate = null,
        Func<IQueryable<ChatHistory>, IOrderedQueryable<ChatHistory>>? orderBy = null,
        int? skip = null,
        int? take = null)
    {
        IQueryable<ChatHistory> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync();
    }

    public async Task<int> CountChatHistoriesAsync(Expression<Func<ChatHistory, bool>>? predicate = null)
    {
        IQueryable<ChatHistory> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<int> GetNewMessagesCountAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.CountAsync(h => h.CreatedAt >= startDate && h.CreatedAt < endDate);
    }
}
