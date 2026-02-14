using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class ChatSessionRepository : Repository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ChatSession>> GetChatSessionsAsync(
        Expression<Func<ChatSession, bool>>? predicate = null,
        Func<IQueryable<ChatSession>, IOrderedQueryable<ChatSession>>? orderBy = null,
        int? skip = null,
        int? take = null,
        bool includeUser = false)
    {
        IQueryable<ChatSession> query = _dbSet;

        if (includeUser)
            query = query.Include(s => s.User);

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

    public async Task<int> CountChatSessionsAsync(Expression<Func<ChatSession, bool>>? predicate = null)
    {
        IQueryable<ChatSession> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<ChatSession?> GetChatSessionWithUserAsync(int sessionId)
    {
        return await _dbSet
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<int> GetNewChatSessionsCountAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.CountAsync(s => s.CreatedAt >= startDate && s.CreatedAt < endDate);
    }

    public async Task DeleteAllUserChatSessionsAsync(int userId)
    {
        var sessions = await _dbSet
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _dbSet.RemoveRange(sessions);
        await _context.SaveChangesAsync();
    }
}
