using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Services
{
    [TestClass]
    public class ServerListServiceTests
    {
        private IpApiClient _ipApiClient;
        private NullLoggerFactory _loggingFactory;
        private StoreServerURLFilter _storeServerURLFilter;
        private IHttpContextAccessor _httpContextAccessor;
        private TraceRouteApiClient _traceRouteApiClient;
        private HomeController _homeController;
        private ServerListService _serverListService;

        public ServerListServiceTests()
        {
            _loggingFactory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });

            _ipApiClient = new(httpClient, _loggingFactory.CreateLogger<IpApiClient>(), memoryCache);
            _traceRouteApiClient = new(httpClient, _loggingFactory.CreateLogger<TraceRouteApiClient>());

            BogonIPService bogonIPService = new(_loggingFactory);
            _homeController = new(bogonIPService, _loggingFactory);

            _storeServerURLFilter = new();
            _httpContextAccessor = ContextAccessorHelper.GetContext("/", "localhost");

            _serverListService = new(_loggingFactory.CreateLogger<ServerListService>(), _ipApiClient, _storeServerURLFilter, _traceRouteApiClient);
        }

        [TestMethod("Can start and stop the service")]
        public async Task StartAndStopAsync()
        {            

            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            
            // Assert first result
            List<ServerEntry> result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].isLocalHost);
            Assert.AreEqual("Localhost", result[0].url);

            // I set the server URL
            Assert.IsNotNull(_httpContextAccessor.HttpContext);
            ActionContext actionContext = new(_httpContextAccessor.HttpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            ActionExecutingContext actionExecutingContext = new(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), _serverListService);

            ActionExecutionDelegate actionExecutionDelegate = () =>
            {
                var ctx = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), _homeController);
                return Task.FromResult(ctx);
            };
            await _storeServerURLFilter.OnActionExecutionAsync(actionExecutingContext, actionExecutionDelegate);

            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            ServerEntry? serverEntry = result.Where(x => x.isLocalHost).FirstOrDefault();            
            Assert.AreNotEqual("Localhost", result[0].url);

            // I stop the service
            await ((IHostedService)_serverListService).StopAsync(new CancellationToken());
            result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod("Can clean the server list")]
        public async Task CleanServerList()
        {
            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            List<ServerEntry> result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            // I add an expired server
            ServerEntry serverEntry = new()
            {
                lastUpdate = DateTime.Now.AddMinutes(-10),
                url = "http://localhost:5000"
            };
            _serverListService.AddServer(serverEntry);
            List<ServerEntry> secondResult = _serverListService.GetServerList();

            Assert.IsTrue(secondResult.Count == result.Count + 1);

            // I clean the server list
            await _serverListService.CleanServerList();

            List<ServerEntry> thirdResult = _serverListService.GetServerList();
            Assert.AreEqual(result.Count, result.Count);
        }

    }
}
