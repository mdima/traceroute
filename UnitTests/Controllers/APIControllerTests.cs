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
            StoreServerURLFilter storeServerURLFilter = new();

            IpApiClient ipApiClient = new(httpClient, factory.CreateLogger<IpApiClient>(), memoryCache);
            TraceRouteApiClient traceRouteApiClient = new(httpClient, factory.CreateLogger<TraceRouteApiClient>());
            ReverseLookupService reverseService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);
            ServerListService serverListService = new(factory.CreateLogger<ServerListService>(), ipApiClient, storeServerURLFilter, traceRouteApiClient);
            _controller = new(factory, ipApiClient, bogonIPService, reverseService, serverListService);
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


        [TestMethod]
        public async Task SecurityChecks()
        {
            TraceResultViewModel response = await _controller.TraceRoute("www.nt2.it;ls /");
            Assert.AreEqual("Error while tracing", response.ErrorDescription);

            TraceHopDetails hopResponse = await _controller.IPInfo("ls /");
            Assert.AreEqual("Could not retrive the IP information", hopResponse.ErrorDescription);
        }

        [TestMethod]
        public async Task IPDetails()
        {
            IpApiResponse response = await _controller.IPDetails("192.188.248.215");

            Assert.AreEqual("192.188.248.215", response.query);
            Assert.AreEqual("success", response.status);
            Assert.AreEqual("Milan", response.city);

            response = await _controller.IPDetails("errorgen");
            Assert.AreEqual("Could not retrive the IP information", response.status);

            response = await _controller.IPDetails("10.0.0.1");
            Assert.AreEqual("BogonIP", response.status);            
        }

        [TestMethod]
        public async Task GetSettings()
        {
            SettingsViewModel response = await _controller.GetSettings();

            Assert.AreEqual("", response.CurrentServerURL);            
            Assert.AreEqual(ConfigurationHelper.GetEnableRemoteTraces(), response.EnableRemoteTraces);
            Assert.AreEqual(ConfigurationHelper.GetHostRemoteTraces(), response.HostRemoteTraces);
            Assert.IsNotNull(response.ServerLocation);
        }

    }
}
