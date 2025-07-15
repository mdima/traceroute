using Blazored.Toast;
using Blazored.Toast.Services;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Components.Layout
{
    [TestClass]
    public class MainLayoutTests : Bunit.TestContext
    {
        private Boolean toastFired = false;

        public MainLayoutTests()
        {
            // Initialize the test context for Bunit
            NullLoggerFactory loggingFactory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            ReverseLookupService reverseLookupService = new(loggingFactory.CreateLogger<ReverseLookupService>(), memoryCache);
            Services.AddSingleton(reverseLookupService);

            IpApiClient ipApiClient;
            ipApiClient = new(httpClient, loggingFactory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
            Services.AddHttpClient<IpApiClient>();

            TraceRouteApiClient _traceRouteApiClient;
            _traceRouteApiClient = new(httpClient, loggingFactory.CreateLogger<TraceRouteApiClient>());
            Services.AddHttpClient<TraceRouteApiClient>();

            BogonIPService bogonIPService = new(loggingFactory);
            Services.AddSingleton(bogonIPService);

            StoreServerURLFilter storeServerURLFilter;
            storeServerURLFilter = new();
            Services.AddSingleton(storeServerURLFilter);

            IHttpContextAccessor httpContextAccessor;
            httpContextAccessor = ContextAccessorHelper.GetContext("/", "localhost");
            Services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            Services.AddHttpContextAccessor();

            ServerListService serverListService;
            serverListService = new(loggingFactory.CreateLogger<ServerListService>(), ipApiClient, storeServerURLFilter, _traceRouteApiClient);
            ((IHostedService)serverListService).StartAsync(new CancellationToken()).GetAwaiter().GetResult();
            Services.AddSingleton(serverListService);

            TracerouteService tracerouteService;            
            tracerouteService = new(bogonIPService, loggingFactory.CreateLogger<TracerouteService>());
            Services.AddSingleton(tracerouteService);

            Services.AddMemoryCache(x => { x.TrackStatistics = true; x.TrackLinkedCacheEntries = true; });

            ToastService toastService = new();
            toastService.OnShow += ToastService_OnShow;
            Services.AddSingleton<IToastService>(toastService);

            JSInterop.Mode = JSRuntimeMode.Loose; // Use loose mode for JS interop in tests
        }

        private void ToastService_OnShow(ToastLevel arg1, Microsoft.AspNetCore.Components.RenderFragment arg2, Action<Blazored.Toast.Configuration.ToastSettings>? arg3)
        {
            toastFired = true;
        }

        [TestMethod]
        public void TestLayout()
        {
            // Arrange a simple render
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();
            Assert.IsNotNull(cut);

            // Check if the header contains the expected text
            Assert.IsTrue(cut.Markup.Contains("TraceRoute"));
        }

        [TestMethod]
        public void TestInitialHost()
        {
            // I reset the default http context
            var context = new DefaultHttpContext();            
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();
            // Check the hostToTrace as empty
            Assert.IsEmpty(cut.Instance.hostToTrace);

            ContextAccessorHelper.GetContext("/", "localhost", "127.0.0.1");
            cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();
            Assert.IsEmpty(cut.Instance.hostToTrace);

            ContextAccessorHelper.GetContext("/", "localhost", "8.8.8.8");
            cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();
            Assert.IsNotEmpty(cut.Instance.hostToTrace);
            Assert.AreEqual("8.8.8.8", cut.Instance.hostToTrace);
        }

        [TestMethod]
        public void TestShowServerEntry()
        {
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();

            // Full detailed ServerEntry
            ServerEntry serverEntry = new ServerEntry
            {
                url = "http://localhost",
                Details = new IpDetails
                {
                    Country = "Country",
                    City = "City"
                }
            };
            String? result = cut.Instance.ShowServerEntry(serverEntry);
            Assert.AreEqual("Country - City - http://localhost", result);

            // Null Country & City
            serverEntry = new ServerEntry
            {
                url = "http://localhost",
                Details = new IpDetails()
            };
            result = cut.Instance.ShowServerEntry(serverEntry);
            Assert.AreEqual("http://localhost", result);
        }

        [TestMethod]
        public void TestRefreshServerList()
        {
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();
            cut.Instance.selectedServerUrl = "not existing server";

            cut.Instance.RefreshServerList();
            Assert.AreEqual("Localhost", cut.Instance.selectedServerUrl);

            // I test it again to check the normal result
            cut.Instance.RefreshServerList();
            Assert.AreEqual("Localhost", cut.Instance.selectedServerUrl);
        }

        [TestMethod]
        public async Task TestBeginTraceRoute()
        {
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();
            cut.Instance.selectedServerUrl = "not existing server";
            cut.Instance.hostToTrace = "127.0.0.1";
            toastFired = false;

            // Invalid server selected
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);
            Assert.IsTrue(toastFired);
            toastFired = false;

            // Local server selected
            cut.Instance.selectedServerUrl = cut.Instance.serverList.First().url;
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);
            Assert.IsFalse(toastFired);
        }
    }
}
