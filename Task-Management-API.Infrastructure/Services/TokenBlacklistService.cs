using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Task_Management_Api.Application.Interfaces;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;

    public TokenBlacklistService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task AddTokenToBlacklistAsync(string token, DateTime expiresAt)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiresAt
        };

        await _cache.SetStringAsync(token, "blacklisted", options);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        var result = await _cache.GetStringAsync(token);
        return result != null;
    }
}
