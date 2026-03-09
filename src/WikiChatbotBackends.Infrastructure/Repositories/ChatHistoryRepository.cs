using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WikiChatbotBackends.Application.DTOs;
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

    public async Task<Dictionary<int, int>> GetMessageCountByUserAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbSet.Include(h => h.Session).AsQueryable();

        if (startDate.HasValue)
            query = query.Where(h => h.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(h => h.CreatedAt < endDate.Value);

        var result = await query
            .GroupBy(h => h.Session.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        return result;
    }

    public async Task<List<TimeSeriesStatsDto>> GetTimeSeriesStatsAsync(DateTime startDate, DateTime endDate, TimeGrouping grouping)
    {
        var stats = new List<TimeSeriesStatsDto>();

        var messages = await _dbSet
            .Include(h => h.Session)
            .Where(h => h.CreatedAt >= startDate && h.CreatedAt < endDate)
            .ToListAsync();

        var sessions = await _context.ChatSessions
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt < endDate)
            .ToListAsync();

        var users = await _context.Users
            .Where(u => u.CreatedAt >= startDate && u.CreatedAt < endDate)
            .ToListAsync();

        // Group by period based on grouping parameter
        var currentDate = grouping switch
        {
            TimeGrouping.Day => startDate.Date,
            TimeGrouping.Week => GetStartOfWeek(startDate),
            TimeGrouping.Month => new DateTime(startDate.Year, startDate.Month, 1),
            _ => startDate.Date
        };

        while (currentDate <= endDate)
        {
            var nextDate = grouping switch
            {
                TimeGrouping.Day => currentDate.AddDays(1),
                TimeGrouping.Week => currentDate.AddDays(7),
                TimeGrouping.Month => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };

            var periodMessages = messages.Count(m => m.CreatedAt >= currentDate && m.CreatedAt < nextDate);
            var periodSessions = sessions.Count(s => s.CreatedAt >= currentDate && s.CreatedAt < nextDate);
            var periodUsers = users.Count(u => u.CreatedAt >= currentDate && u.CreatedAt < nextDate);

            stats.Add(new TimeSeriesStatsDto
            {
                Period = currentDate,
                PeriodLabel = GetPeriodLabel(currentDate, grouping),
                MessageCount = periodMessages,
                SessionCount = periodSessions,
                UserCount = periodUsers
            });

            currentDate = nextDate;
        }

        return stats;
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private string GetPeriodLabel(DateTime date, TimeGrouping grouping)
    {
        return grouping switch
        {
            TimeGrouping.Day => date.ToString("yyyy-MM-dd"),
            TimeGrouping.Week => $"W{GetWeekNumber(date)}/{date.Year}",
            TimeGrouping.Month => date.ToString("yyyy-MM"),
            _ => date.ToString("yyyy-MM-dd")
        };
    }

    private int GetWeekNumber(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}

