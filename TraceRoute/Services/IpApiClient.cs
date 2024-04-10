using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Net;
using TraceRoute.Helpers;
using TraceRoute.Models;

namespace TraceRoute.Services
{
    /// <summary>
    /// Service that interacts with IP-API.com
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="logger">The Logger service</param>
    /// <param name="MemoryCache">The Memory Cache service</param>
    public class IpApiClient(HttpClient httpClient, ILogger<IpApiClient> logger, IMemoryCache MemoryCache)
    {
        private const string BASE_URL = "http://ip-api.com";
        private readonly HttpClient _httpClient = httpClient;
        private readonly IMemoryCache _MemoryCache = MemoryCache;
        private readonly ILogger _logger = logger;
        
        /// <summary>
        /// Retrives the IP information from IP-API.com
        /// </summary>
        /// <param name="ipAddress">The IP Address to query</param>
        /// <param name="ct">The cancellation token for an async operation</param>
        /// <returns>The IP address information</returns>
        public async Task<IpApiResponse?> Get(string? ipAddress, CancellationToken ct = default)
        {
            string cacheName = "IP_Info_" + ipAddress;

            try
            {
                IpApiResponse? response = _MemoryCache.Get<IpApiResponse>(cacheName);
                if (response == null)
                {
                    _logger.LogDebug("Asking the IP information for IP: {0}", ipAddress);
                    string route = $"{BASE_URL}/json/{ipAddress}";
                    response = await _httpClient.GetFromJsonAsync<IpApiResponse>(route, ct);
                    _logger.LogDebug("Result: {0}", JsonConvert.SerializeObject(response));
                    if (response != null)
                    {
                        _MemoryCache.Set(cacheName, response, DateTimeOffset.Now.AddMinutes(10));
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting the IP information for IP: {0}, Err: {1}", ipAddress, ex.Message);
                return null;
            }
        }
    }
}
