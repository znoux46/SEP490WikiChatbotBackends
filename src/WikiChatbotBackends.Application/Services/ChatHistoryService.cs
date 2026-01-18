using WikiChatbotBackends.Application.DTOs;
using WikiChatbotBackends.Application.Interfaces;
using WikiChatbotBackends.Domain.Entities;

namespace WikiChatbotBackends.Application.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly IRepository<ChatSession> _sessionRepository;
    private readonly IRepository<ChatHistory> _chatHistoryRepository;

    public ChatHistoryService(
        IRepository<ChatSession> sessionRepository,
        IRepository<ChatHistory> chatHistoryRepository)
    {
        _sessionRepository = sessionRepository;
        _chatHistoryRepository = chatHistoryRepository;
    }

    // Session methods
    public async Task<IEnumerable<SessionSummaryDto>> GetUserSessionsAsync(int userId)
    {
        var sessions = await _sessionRepository.FindAsync(s => s.UserId == userId);
        
        return sessions.Select(session => new SessionSummaryDto
        {
            Id = session.Id,
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            CreatedAt = session.CreatedAt,
            LastMessageAt = session.ChatHistories.Any() 
                ? session.ChatHistories.Max(h => h.CreatedAt) 
                : session.CreatedAt,
            MessageCount = session.ChatHistories.Count
        }).OrderByDescending(s => s.LastMessageAt);
    }

    public async Task<ChatSessionDto> GetSessionByIdAsync(int userId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        return new ChatSessionDto
        {
            Id = session.Id,
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<ChatSessionDto> CreateSessionAsync(int userId, CreateChatSessionDto dto)
    {
        var session = new ChatSession
        {
            UserId = userId,
            SessionId = dto.SessionId,
            SessionName = dto.SessionName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _sessionRepository.AddAsync(session);
        return new ChatSessionDto
        {
            Id = created.Id,
            SessionId = created.SessionId,
            SessionName = created.SessionName,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }

    public async Task<ChatSessionDto> UpdateSessionAsync(int userId, int sessionId, UpdateChatSessionDto dto)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        session.SessionName = dto.SessionName;
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session);
        return new ChatSessionDto
        {
            Id = session.Id,
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task DeleteSessionAsync(int userId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        // Xóa tất cả chat histories trước (hoặc để cascade delete)
        var histories = await _chatHistoryRepository.FindAsync(ch => ch.SessionId == sessionId);
        foreach (var history in histories)
        {
            await _chatHistoryRepository.DeleteAsync(history);
        }

        await _sessionRepository.DeleteAsync(session);
    }

    public async Task ClearAllSessionsAsync(int userId)
    {
        var sessions = await _sessionRepository.FindAsync(s => s.UserId == userId);
        foreach (var session in sessions)
        {
            // Xóa histories trước
            var histories = await _chatHistoryRepository.FindAsync(ch => ch.SessionId == session.Id);
            foreach (var history in histories)
            {
                await _chatHistoryRepository.DeleteAsync(history);
            }
            await _sessionRepository.DeleteAsync(session);
        }
    }

    // History methods
    public async Task<IEnumerable<ChatHistoryDto>> GetSessionHistoryAsync(int userId, int sessionId)
    {
        // Verify session belongs to user
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {sessionId} not found");

        var histories = await _chatHistoryRepository.FindAsync(ch => ch.SessionId == sessionId);
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
        // Verify session belongs to user
        var session = await _sessionRepository.GetByIdAsync(dto.SessionId);
        if (session == null || session.UserId != userId)
            throw new KeyNotFoundException($"Session with id {dto.SessionId} not found");

        var chatHistory = new ChatHistory
        {
            SessionId = dto.SessionId,
            Question = dto.Question,
            Answer = dto.Answer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _chatHistoryRepository.AddAsync(chatHistory);
        
        // Update session UpdatedAt
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);

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
        if (history == null)
            throw new KeyNotFoundException($"Chat history with id {historyId} not found");

        // Verify session belongs to user
        var session = await _sessionRepository.GetByIdAsync(history.SessionId);
        if (session == null || session.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to update this history");

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

    public async Task DeleteChatHistoryAsync(int userId, int historyId)
    {
        var history = await _chatHistoryRepository.GetByIdAsync(historyId);
        if (history == null)
            throw new KeyNotFoundException($"Chat history with id {historyId} not found");

        // Verify session belongs to user
        var session = await _sessionRepository.GetByIdAsync(history.SessionId);
        if (session == null || session.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this history");

        await _chatHistoryRepository.DeleteAsync(history);
    }
}