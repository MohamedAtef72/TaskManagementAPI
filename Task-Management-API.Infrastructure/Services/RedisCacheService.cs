using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Task_Management_Api.Application.Interfaces;

namespace Task_Management_API.Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly TimeSpan _defaultExpiration;
        private readonly string _instanceName;

        public RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger, IConfiguration configuration)
        {
            _distributedCache = distributedCache;
            _logger = logger;

            // Read TimeSpan from configuration
            var expirationString = configuration["Redis:DefaultCacheExpiration"];
            _defaultExpiration = TimeSpan.Parse(expirationString ?? "00:15:00");
            _instanceName = configuration["Redis:InstanceName"] ?? "TaskManagementAPI";

        }
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var cachedValue = await _distributedCache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedValue))
                    return null;

                return JsonSerializer.Deserialize<T>(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data from cache with key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
                };

                await _distributedCache.SetStringAsync(key, serializedValue, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting data to cache with key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing data from cache with key: {Key}", key);
            }
        }
    }
}
