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
using static TraceRoute.Models.TraceResultViewModel;

namespace UnitTests.Components.Layout
{
    [TestClass]
    public class MainLayoutTests : Bunit.TestContext
    {
        private Boolean toastFired = false;
        IHttpContextAccessor httpContextAccessor;
        MemoryCache memoryCache;

        public MainLayoutTests()
        {
            // Initialize the test context for Bunit
            NullLoggerFactory loggingFactory = new();
            HttpClient httpClient = new HttpClient();
            memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            ReverseLookupService reverseLookupService = new(loggingFactory.CreateLogger<ReverseLookupService>(), memoryCache);
            Services.AddSingleton(reverseLookupService);

            IpApiClient ipApiClient = new(httpClient, loggingFactory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
            Services.AddHttpClient<IpApiClient>();

            TraceRouteApiClient _traceRouteApiClient;
            _traceRouteApiClient = new(httpClient, loggingFactory.CreateLogger<TraceRouteApiClient>());
            Services.AddHttpClient<TraceRouteApiClient>();

            BogonIPService bogonIPService = new(loggingFactory);
            Services.AddSingleton(bogonIPService);

            StoreServerURLFilter storeServerURLFilter;
            storeServerURLFilter = new();
            Services.AddSingleton(storeServerURLFilter);

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

            // I empty the context
            httpContextAccessor = new HttpContextAccessor() { HttpContext = null };
            cut.Instance.hostToTrace = "";
            cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();
            Assert.IsEmpty(cut.Instance.hostToTrace);
            httpContextAccessor = ContextAccessorHelper.GetContext("/", "localhost");
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
            cut.WaitForAssertion(() =>
            {
                Assert.IsNotNull(cut.Instance.traceResult);
                Assert.IsTrue(cut.Instance.traceResult.Hops.Count > 0);
            });
            Assert.IsTrue(cut.Instance.traceResult!.Hops.First().Details.IsBogonIP);
            Assert.IsFalse(cut.Instance.traceResult!.Hops.Last().Details.IsBogonIP);

            // remote server selected
            cut.Instance.serverList.Add(new ServerEntry
            {
                url = "https://traceroute.di-maria.it/",
                isLocalHost = false,
                isOnline = true,
                Details = new IpDetails
                {
                    Country = "Italy",
                    City = "Milan",                    
                }
            });
            cut.Instance.selectedServerUrl = "https://traceroute.di-maria.it/";
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);
            Assert.IsFalse(toastFired);

            // I generate a trace error
            toastFired = false;
            cut.Instance.selectedServerUrl = cut.Instance.serverList.First().url;
            cut.Instance.hostToTrace = Guid.NewGuid().ToString();
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);
            Assert.IsTrue(toastFired);

            // I make sure I cannot get the trace hop details
            IpApiClient.BASE_URL = "aaaa";
            cut.Instance.hostToTrace = "8.8.8.8";
            memoryCache.Clear();    // I need to empty the memory cache
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);          
            Assert.IsNull(cut.Instance.traceResult.Hops.Where(x => x.HopAddress == "8.8.8.8").First().Details.Country);
            IpApiClient.BASE_URL = "http://ip-api.com";
        }

        [TestMethod]
        public async Task TestOnShowHopDetails()
        {
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();

            cut.Instance.traceResult = new TraceResultViewModel();
            TraceHop hop = new()
            {
                HopAddress = Guid.NewGuid().ToString(),
                Details = new IpDetails
                {
                    Country = "Country",
                    City = "City",
                }
            };
            await cut.InvokeAsync(() => cut.Instance.OnShowHopDetails(hop));
            Assert.AreEqual(hop, cut.Instance.currentHop);
        }

        [TestMethod]
        public async Task TestOnShowIpDetails()
        {
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();

            // Null result
            cut.Instance.traceResult = null;
            await cut.InvokeAsync(() => cut.Instance.OnShowIpDetails("asdf"));

            // Empty result
            toastFired = false;
            cut.Instance.traceResult = new TraceResultViewModel();
            await cut.InvokeAsync(() => cut.Instance.OnShowIpDetails("asdf"));
            Assert.IsTrue(toastFired);

            // Valid result
            toastFired = false;
            cut.Instance.traceResult.Hops.Add(new TraceHop()
            {
                HopAddress = "127.0.0.1",
                Details = new IpDetails
                {
                    Country = "Country",
                    City = "City",
                }
            });
            await cut.InvokeAsync(() => cut.Instance.OnShowIpDetails("127.0.0.1"));
            Assert.IsFalse(toastFired);
            Assert.AreEqual(cut.Instance.traceResult.Hops.First(), cut.Instance.currentHop);
        }

        [TestMethod]
        public async Task TestShowServerDetails()
        {
            var cut = RenderComponent<TraceRoute.Components.Layout.MainLayout>();

            // Non existing server
            cut.Instance.currentHop = null;
            cut.Instance.selectedServerUrl = Guid.NewGuid().ToString();
            await cut.InvokeAsync(() => cut.Instance.ShowServerDetails());
            Assert.IsNull(cut.Instance.currentHop);

            // Existing server
            cut.Instance.selectedServerUrl = cut.Instance.serverList.First().url;
            await cut.InvokeAsync(() => cut.Instance.ShowServerDetails());
            Assert.IsNotNull(cut.Instance.currentHop);
            Assert.AreEqual(cut.Instance.selectedServerUrl, cut.Instance.currentHop.HopAddress);
        }
    }
}
