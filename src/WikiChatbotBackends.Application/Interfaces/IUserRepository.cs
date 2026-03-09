using System.Linq.Expressions;
using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<IEnumerable<User>> GetUsersAsync(
        Expression<Func<User, bool>>? predicate = null,
        Func<IQueryable<User>, IOrderedQueryable<User>>? orderBy = null,
        int? skip = null,
        int? take = null);
    
    Task<int> CountUsersAsync(Expression<Func<User, bool>>? predicate = null);
    
    Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null);
    
    Task<(int totalUsers, int totalAdmins, int totalRegularUsers)> GetUserStatisticsAsync();
    
    Task<int> GetNewUsersCountAsync(DateTime startDate, DateTime endDate);
    
    Task<List<UserGrowthStatsDto>> GetUserGrowthStatsAsync(DateTime startDate, DateTime endDate);
    
    Task<Dictionary<int, int>> GetSessionCountByUserAsync(DateTime? startDate = null, DateTime? endDate = null);
}
