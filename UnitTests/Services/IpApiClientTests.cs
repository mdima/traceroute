using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Services
{

    public class IpApiClientTests
    {
        private IpApiClient _ipApiClient;

        public IpApiClientTests() {
            NullLoggerFactory factory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            ReverseLookupService reverseLookupService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);

            _ipApiClient = new(httpClient, factory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
        }

        [Fact]
        public async Task GetKnownIP()
        {
            IpApiResponse? result = await _ipApiClient.Get("192.188.248.215");

            Assert.NotNull(result);
            Assert.Equal("success", result.status);
            Assert.Equal("Milan", result.city);
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
    }
}
