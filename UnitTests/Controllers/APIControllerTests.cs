using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
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
        private ServerListService _serverListService;

        public APIControllerTests()
        {
            NullLoggerFactory factory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            BogonIPService bogonIPService = new(factory);
            StoreServerURLFilter storeServerURLFilter = new();
            ReverseLookupService reverseService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);

            IpApiClient ipApiClient = new(httpClient, factory.CreateLogger<IpApiClient>(), memoryCache, reverseService);
            TraceRouteApiClient traceRouteApiClient = new(httpClient, factory.CreateLogger<TraceRouteApiClient>());
            TracerouteService tracerouteService = new(bogonIPService, factory.CreateLogger<TracerouteService>());
            _serverListService = new(factory.CreateLogger<ServerListService>(), ipApiClient, storeServerURLFilter, traceRouteApiClient);
            _controller = new(factory, _serverListService, tracerouteService);
        }

        [TestMethod]
        public async Task TraceRouteOK()
        {
            List<string>? response = await _controller.TraceRoute("192.188.248.215");

            Assert.IsNotNull(response);
            Assert.IsTrue(response.Count >= 2);

            response = await _controller.TraceRoute("127.0.0.1");
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Count >= 1);

            String hop = response.First();
            Assert.AreEqual("127.0.0.1", hop);
        }

        [TestMethod]
        public async Task SecurityChecks()
        {
            List<string>? response = await _controller.TraceRoute("www.nt2.it;ls /");
            Assert.IsNull(response);
        }

        [TestMethod]
        public async Task ReceivePresence()
        {
            ServerEntry server = new();
            server.url = ConfigurationHelper.GetRootNode();

            bool result = await _controller.ReceivePresence(server);

            Assert.IsFalse(result);

            ServerEntry? rootServer = await _serverListService.GetRemoteServerInfo(server);
            Assert.IsNotNull(rootServer);
            Assert.AreEqual(server.url, rootServer.url);
            result = await _controller.ReceivePresence(rootServer);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task GetServerList()
        {
            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            ServerEntry? server = _serverListService.GetCurrentServerInfo();
            Assert.IsNotNull(server);

            List<ServerEntry> serverList = _controller.GetServerList();
            Assert.IsNotNull(serverList);

            Assert.IsTrue(serverList.Contains(server));
        }

        [TestMethod]
        public async Task GetServerInfo()
        {
            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            ServerEntry? server = _serverListService.GetCurrentServerInfo();
            Assert.IsNotNull(server);

            ServerEntry? serverInfo = _controller.GetServerInfo();
            Assert.IsNotNull(serverInfo);

            Assert.AreEqual(server, serverInfo);
        }
    }
}
