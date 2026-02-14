using System.Linq.Expressions;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<IEnumerable<ChatSession>> GetChatSessionsAsync(
        Expression<Func<ChatSession, bool>>? predicate = null,
        Func<IQueryable<ChatSession>, IOrderedQueryable<ChatSession>>? orderBy = null,
        int? skip = null,
        int? take = null,
        bool includeUser = false);
    
    Task<int> CountChatSessionsAsync(Expression<Func<ChatSession, bool>>? predicate = null);
    
    Task<ChatSession?> GetChatSessionWithUserAsync(int sessionId);
    
    Task<int> GetNewChatSessionsCountAsync(DateTime startDate, DateTime endDate);
    
    Task DeleteAllUserChatSessionsAsync(int userId);
}
