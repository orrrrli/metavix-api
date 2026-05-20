using Application.Common.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services;

public sealed class LoginAttemptTracker : ILoginAttemptTracker
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public LoginAttemptTracker(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsBlocked(string email)
    {
        return _cache.TryGetValue(BlockKey(email), out _);
    }

    public void RegisterFailure(string email)
    {
        string countKey = CountKey(email);

        int attempts = _cache.GetOrCreate(countKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = LockoutWindow;
            return 0;
        });

        attempts++;

        if (attempts >= MaxFailures)
        {
            _cache.Set(BlockKey(email), true, LockoutWindow);
            _cache.Remove(countKey);
        }
        else
        {
            _cache.Set(countKey, attempts, LockoutWindow);
        }
    }

    public void ResetAttempts(string email)
    {
        _cache.Remove(CountKey(email));
        _cache.Remove(BlockKey(email));
    }

    private static string CountKey(string email) => $"login:attempts:{email.ToLowerInvariant()}";
    private static string BlockKey(string email) => $"login:blocked:{email.ToLowerInvariant()}";
}
