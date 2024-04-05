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
    [TestClass]
    public class IpApiClientTests
    {
        private IpApiClient _ipApiClient;

        public IpApiClientTests() {
            NullLoggerFactory factory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });

            _ipApiClient = new(httpClient, factory.CreateLogger<IpApiClient>(), memoryCache);
        }

        [TestMethod]
        public async Task GetKnownIP()
        {

            IpApiResponse? result = await _ipApiClient.Get("192.188.248.215");

            Assert.IsNotNull(result);
            Assert.AreEqual("success", result.status);
            Assert.AreEqual("Milan", result.city);
            Assert.AreEqual("Italy", result.country);
            Assert.AreEqual("Lombardy", result.regionName);
            Assert.IsNotNull(result.lat);
            Assert.IsNotNull(result.lon);
            Assert.IsNotNull(result.isp);
            Assert.IsNotNull(result.zip);
            Assert.IsNull(result.continent);
            Assert.IsNull(result.district);
        }

        [TestMethod]
        public async Task GetUnKnownIP()
        {

            IpApiResponse? result = await _ipApiClient.Get("192.168.0.1");

            Assert.IsNotNull(result);
            Assert.AreEqual("fail", result.status);
        }

        [TestMethod]
        public async Task GetErrorIP()
        {

            IpApiResponse? result = await _ipApiClient.Get("qwerty");

            Assert.IsNotNull(result);
            Assert.AreEqual("fail", result.status);
        }
    }
}
