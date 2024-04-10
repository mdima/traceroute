using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

namespace UnitTests.Controllers
{
    [TestClass]
    public class APIControllerTests
    {
        private APIController _controller;

        public APIControllerTests()
        {
            NullLoggerFactory factory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            BogonIPService bogonIPService = new(factory);

            IpApiClient ipApiClient = new(httpClient, factory.CreateLogger<IpApiClient>(), memoryCache);
            ReverseLookupService reverseService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);

            _controller = new(factory, ipApiClient, bogonIPService, reverseService);
        }

        [TestMethod]
        public async Task TraceRouteOK()
        {
            TraceResultViewModel response = await _controller.TraceRoute("192.188.248.215");

            Assert.AreEqual("", response.ErrorDescription);
            Assert.IsTrue(response.Hops.Count >= 2);

            response = await _controller.TraceRoute("127.0.0.1");
            Assert.AreEqual("", response.ErrorDescription);
            Assert.IsTrue(response.Hops.Count >= 1);
        }

        [TestMethod]
        public async Task IPInfo()
        {
            TraceHopDetails response = await _controller.IPInfo("192.188.248.215");

            Assert.AreEqual("", response.ErrorDescription);
            Assert.AreEqual("Milan", response.City);

            response = await _controller.IPInfo("errorgen");
            Assert.IsNotNull(response.ErrorDescription);

            response = await _controller.IPInfo("10.0.0.1");
            Assert.AreEqual("", response.ErrorDescription);
            Assert.IsTrue(response.IsBogonIP);
        }
    }
}
