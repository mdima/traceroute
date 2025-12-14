using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Services
{

    public class IpApiClientTests
    {
        private IpApiClient _ipApiClient;
        NullLoggerFactory factory = new();
        MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
        ReverseLookupService reverseLookupService;

        public IpApiClientTests() {
            
            HttpClient httpClient = new HttpClient();            
            reverseLookupService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);

            _ipApiClient = new(httpClient, factory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
        }

        [Fact]
        public async Task GetKnownIP()
        {
            IpApiResponse? result = await _ipApiClient.Get("192.188.248.215");

            Assert.NotNull(result);
            Assert.Equal("success", result.status);            
            Assert.Equal("Zona Industriale", result.city); // Assert.Equal("Milan", result.city); ??
            Assert.Equal("Italy", result.country);
            Assert.Equal("Lombardy", result.regionName);
            Assert.Equal("Europe", result.continent);
            Assert.NotNull(result.lat);
            Assert.NotNull(result.lon);
            Assert.NotNull(result.isp);
            Assert.NotNull(result.zip);
            Assert.NotNull(result.district);
        }

        [Fact]
        public async Task GetUnKnownIP()
        {
            IpApiResponse? result = await _ipApiClient.Get("192.168.0.1");

            Assert.NotNull(result);
            Assert.Equal("fail", result.status);
        }

        [Fact]
        public async Task GetErrorIP()
        {
            IpApiResponse? result = await _ipApiClient.Get("qwerty");

            Assert.Null(result);
        }

        [Fact]
        public async Task TestAPIQuota()
        {
            // Normal result 
            IpApiResponse? result = await _ipApiClient.Get("192.188.248.215"); 
            Assert.NotNull(result);

            Assert.NotNull(_ipApiClient.requestLimit);
            int requestLimit = int.Parse(_ipApiClient.requestLimit);
            Assert.True(requestLimit > 0);

            Assert.True(_ipApiClient._quotaReset < DateTime.Now);

            // Simulate limit exeeded
            _ipApiClient._quotaReset = DateTime.Now.AddMinutes(1);
            result = await _ipApiClient.Get("192.188.248.216");
            Assert.Null(result);
            _ipApiClient._quotaReset = DateTime.MinValue;

            // Simulate quota hit
            var overrideHandler = new OverrideHeadersHandler(
                new Dictionary<string, string>
                {
                    ["X-Rl"] = "0",
                    ["X-Ttl"] = "60"
                }
            )
            {
                InnerHandler = new HttpClientHandler()
            }; ;
            var fakeHttpClient = new HttpClient(overrideHandler);
            IpApiClient fakeApiClient = new(fakeHttpClient, factory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
            result = await fakeApiClient.Get("192.188.248.217");

            Assert.NotNull(result);
            Assert.NotNull(fakeApiClient.requestLimit);
            Assert.Equal("0", fakeApiClient.requestLimit);

            // I double check the quota exeeded condition
            result = await fakeApiClient.Get("192.188.248.218");
            Assert.Null(result);
        }
    }
}
