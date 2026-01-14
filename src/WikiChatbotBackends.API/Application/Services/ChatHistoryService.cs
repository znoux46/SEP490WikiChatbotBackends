using WikiChatbotBackends.API.Application.DTOs;
using WikiChatbotBackends.API.Application.Interfaces;
using WikiChatbotBackends.API.Domain.Entities;

namespace WikiChatbotBackends.API.Application.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly IRepository<ChatHistory> _chatHistoryRepository;

    public ChatHistoryService(IRepository<ChatHistory> chatHistoryRepository)
    {
        _chatHistoryRepository = chatHistoryRepository;
    }

    public async Task<IEnumerable<SessionSummaryDto>> GetUserSessionsAsync(int userId)
    {
        var histories = await _chatHistoryRepository.FindAsync(ch => ch.UserId == userId);
        var grouped = histories.GroupBy(ch => ch.SessionId);

        return grouped.Select(g => new SessionSummaryDto
        {
            SessionId = g.Key,
            LastMessageAt = g.Max(ch => ch.CreatedAt),
            MessageCount = g.Count()
        }).OrderByDescending(s => s.LastMessageAt);
    }

    public async Task<IEnumerable<ChatHistoryDto>> GetSessionHistoryAsync(int userId, string sessionId)
    {
        var histories = await _chatHistoryRepository.FindAsync(ch => ch.UserId == userId && ch.SessionId == sessionId);
        return histories.OrderBy(ch => ch.CreatedAt).Select(ch => new ChatHistoryDto
        {
            Id = ch.Id,
            SessionId = ch.SessionId,
            Question = ch.Question,
            Answer = ch.Answer,
            CreatedAt = ch.CreatedAt
        });
    }

    public async Task<ChatHistoryDto> CreateChatHistoryAsync(int userId, CreateChatHistoryDto dto)
    {
        var chatHistory = new ChatHistory
        {
            UserId = userId,
            SessionId = dto.SessionId,
            Question = dto.Question,
            Answer = dto.Answer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _chatHistoryRepository.AddAsync(chatHistory);
        return new ChatHistoryDto
        {
            Id = created.Id,
            SessionId = created.SessionId,
            Question = created.Question,
            Answer = created.Answer,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<ChatHistoryDto> UpdateChatHistoryAsync(int userId, int historyId, UpdateChatHistoryDto dto)
    {
        var history = await _chatHistoryRepository.GetByIdAsync(historyId);
        if (history == null || history.UserId != userId)
            throw new KeyNotFoundException($"Chat history with id {historyId} not found");

        history.Question = dto.Question;
        history.Answer = dto.Answer;
        history.UpdatedAt = DateTime.UtcNow;

        await _chatHistoryRepository.UpdateAsync(history);
        return new ChatHistoryDto
        {
            Id = history.Id,
            SessionId = history.SessionId,
            Question = history.Question,
            Answer = history.Answer,
            CreatedAt = history.CreatedAt
        };
    }

    public async Task DeleteSessionAsync(int userId, string sessionId)
    {
        var histories = await _chatHistoryRepository.FindAsync(ch => ch.UserId == userId && ch.SessionId == sessionId);
        foreach (var history in histories)
        {
            await _chatHistoryRepository.DeleteAsync(history);
        }
    }

    public async Task ClearAllHistoryAsync(int userId)
    {
        var histories = await _chatHistoryRepository.FindAsync(ch => ch.UserId == userId);
        foreach (var history in histories)
        {
            await _chatHistoryRepository.DeleteAsync(history);
        }
    }
}
