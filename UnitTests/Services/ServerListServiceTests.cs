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
using System.Text;
using System.Threading.Tasks;
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

        public ServerListServiceTests()
        {
            _loggingFactory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });

            _ipApiClient = new(httpClient, _loggingFactory.CreateLogger<IpApiClient>(), memoryCache);
            _traceRouteApiClient = new(httpClient, _loggingFactory.CreateLogger<TraceRouteApiClient>());

            _storeServerURLFilter = new();
            _httpContextAccessor = ContextAccessorHelper.GetContext("/", "localhost");
        }

        [TestMethod("Can start and stop the service")]
        public async Task StartAndStopAsync()
        {
            ServerListService serverListService = new(_loggingFactory.CreateLogger<ServerListService>(), _ipApiClient, _storeServerURLFilter, _traceRouteApiClient);

            await ((IHostedService)serverListService).StartAsync(new CancellationToken());
            
            // Assert first result
            List<ServerEntry> result = serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // I set the server URL
            Assert.IsNotNull(_httpContextAccessor.HttpContext);
            ActionContext actionContext = new(_httpContextAccessor.HttpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            ActionExecutingContext actionExecutingContext = new(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), serverListService);

            ActionExecutionDelegate actionExecutionDelegate = () => {
                var ctx = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null);
                return Task.FromResult(ctx);
            };
            await _storeServerURLFilter.OnActionExecutionAsync(actionExecutingContext, actionExecutionDelegate);

            await ((IHostedService)serverListService).StartAsync(new CancellationToken());
            result = serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].isLocalHost);

            // I stop the service
            await ((IHostedService)serverListService).StopAsync(new CancellationToken());
            result = serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }
    }
}
