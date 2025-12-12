using BurhaniGuards.Api.ViewModel;
using System.Collections.Concurrent;

namespace BurhaniGuards.Api.Services;

public interface ITokenStore
{
    void StoreToken(string token, CurrentUserViewModel user);
    CurrentUserViewModel? GetUserByToken(string token);
    void RemoveToken(string token);
}

public class TokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, CurrentUserViewModel> _tokens = new();
    private readonly Timer _cleanupTimer;

    public TokenStore()
    {
        // Clean up tokens every hour (tokens should expire after some time)
        _cleanupTimer = new Timer(CleanupExpiredTokens, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    public void StoreToken(string token, CurrentUserViewModel user)
    {
        _tokens.TryAdd(token, user);
    }

    public CurrentUserViewModel? GetUserByToken(string token)
    {
        _tokens.TryGetValue(token, out var user);
        return user;
    }

    public void RemoveToken(string token)
    {
        _tokens.TryRemove(token, out _);
    }

    private void CleanupExpiredTokens(object? state)
    {
        // In a production system, you'd check token expiration times
        // For now, we'll keep all tokens until explicitly removed
    }
}




