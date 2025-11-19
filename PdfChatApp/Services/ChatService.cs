using Microsoft.Extensions.Caching.Memory;
using PdfChatApp.Models;

namespace PdfChatApp.Services;

public interface IChatService
{
    string CreateSession(string pdfContent, string fileName, int pageCount);
    ChatSession? GetSession(string sessionId);
    void AddMessage(string sessionId, ChatMessage message);
    bool SessionExists(string sessionId);
    void CleanupOldSessions();
}

public class ChatService : IChatService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ChatService> _logger;
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(2);

    public ChatService(IMemoryCache cache, ILogger<ChatService> logger)
    {
        _cache = cache;
        _logger = logger;

        // Start cleanup task
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(30));
                CleanupOldSessions();
            }
        });
    }

    public string CreateSession(string pdfContent, string fileName, int pageCount)
    {
        var session = new ChatSession
        {
            SessionId = Guid.NewGuid().ToString(),
            PdfContent = pdfContent,
            FileName = fileName,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = _sessionTimeout,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(session.SessionId, session, cacheOptions);

        _logger.LogInformation("Created new chat session: {SessionId} for file: {FileName} with {PageCount} pages",
            session.SessionId, fileName, pageCount);

        return session.SessionId;
    }

    public ChatSession? GetSession(string sessionId)
    {
        if (_cache.TryGetValue(sessionId, out ChatSession? session))
        {
            if (session != null)
            {
                session.LastActivity = DateTime.UtcNow;
                return session;
            }
        }

        _logger.LogWarning("Session not found: {SessionId}", sessionId);
        return null;
    }

    public void AddMessage(string sessionId, ChatMessage message)
    {
        var session = GetSession(sessionId);
        if (session != null)
        {
            session.History.Add(message);
            session.LastActivity = DateTime.UtcNow;

            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = _sessionTimeout,
                Priority = CacheItemPriority.Normal
            };
            _cache.Set(sessionId, session, cacheOptions);

            _logger.LogDebug("Added message to session {SessionId}. Total messages: {Count}",
                sessionId, session.History.Count);
        }
    }

    public bool SessionExists(string sessionId)
    {
        return _cache.TryGetValue(sessionId, out ChatSession? _);
    }

    public void CleanupOldSessions()
    {
        _logger.LogInformation("Running session cleanup task");
        
    }
}