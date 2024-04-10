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
    public class ReverseLookupServiceTests
    {
        private ReverseLookupService _reverseService;

        public ReverseLookupServiceTests() {
            NullLoggerFactory factory = new();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });

            _reverseService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);
        }

        /// <summary>
        ///     This still cannot work because of this issue:
        ///     https://github.com/docker/for-win/issues/13681
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetHostName()
        {
            string result = await _reverseService.GetHostName("192.188.248.215");

            Assert.IsNotNull(result);
            Assert.AreEqual("", result);
        }
    }
}
