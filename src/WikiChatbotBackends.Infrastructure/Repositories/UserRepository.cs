using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;
using WikiChatbotBackends.Infrastructure.Data;

namespace WikiChatbotBackends.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<User>> GetUsersAsync(
        Expression<Func<User, bool>>? predicate = null,
        Func<IQueryable<User>, IOrderedQueryable<User>>? orderBy = null,
        int? skip = null,
        int? take = null)
    {
        IQueryable<User> query = _dbSet;

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

    public async Task<int> CountUsersAsync(Expression<Func<User, bool>>? predicate = null)
    {
        IQueryable<User> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null)
    {
        IQueryable<User> query = _dbSet.Where(u => u.Email == email);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync();
    }

    public async Task<(int totalUsers, int totalAdmins, int totalRegularUsers)> GetUserStatisticsAsync()
    {
        var totalUsers = await _dbSet.CountAsync();
        var totalAdmins = await _dbSet.CountAsync(u => u.Role == "Admin");
        var totalRegularUsers = await _dbSet.CountAsync(u => u.Role == "User");

        return (totalUsers, totalAdmins, totalRegularUsers);
    }

    public async Task<int> GetNewUsersCountAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt < endDate);
    }
}
