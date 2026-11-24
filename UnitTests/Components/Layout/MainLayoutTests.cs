using Blazored.Toast;
using Blazored.Toast.Services;
using Bunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Models;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

namespace UnitTests.Components.Layout
{    
    public class MainLayoutTests : BunitContext
    {
        private Boolean toastFired = false;
        IHttpContextAccessor httpContextAccessor;
        private readonly MemoryCache memoryCache;

        public MainLayoutTests()
        {
            // Initialize the test context for Bunit
            NullLoggerFactory loggingFactory = new();            
            memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            ReverseLookupService reverseLookupService = new(loggingFactory.CreateLogger<ReverseLookupService>(), memoryCache);
            Services.AddSingleton(reverseLookupService);

            Services.ConfigureHttpClientDefaults(http => { http.ConfigureHttpClient(client => { client.Timeout = TimeSpan.FromSeconds(5); }); });
            Services.AddHttpClient();
            IHttpClientFactory factory = Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

            IpApiClient ipApiClient = new(factory, loggingFactory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
            Services.AddSingleton(ipApiClient);

            TraceRouteApiClient _traceRouteApiClient;
            _traceRouteApiClient = new(factory, loggingFactory.CreateLogger<TraceRouteApiClient>());
            Services.AddSingleton<TraceRouteApiClient>();

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

        [Fact]
        public void TestLayout()
        {
            // Arrange a simple render
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();
            Assert.NotNull(cut);

            // Check if the header contains the expected text
            Assert.Contains("TraceRoute", cut.Markup);
        }

        [Fact]
        public void TestInitialHost()
        {
            // I reset the default http context
            var context = new DefaultHttpContext();
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();
            // Check the hostToTrace as empty
            Assert.Empty(cut.Instance.hostToTrace);

            ContextAccessorHelper.GetContext("/", "localhost", "127.0.0.1");
            cut = Render<TraceRoute.Components.Layout.MainLayout>();
            Assert.Empty(cut.Instance.hostToTrace);

            ContextAccessorHelper.GetContext("/", "localhost", "8.8.8.8");
            cut = Render<TraceRoute.Components.Layout.MainLayout>();
            Assert.NotEmpty(cut.Instance.hostToTrace);
            Assert.Equal("8.8.8.8", cut.Instance.hostToTrace);

            // I empty the context
            httpContextAccessor = new HttpContextAccessor() { HttpContext = null };
            cut.Instance.hostToTrace = "";
            cut = Render<TraceRoute.Components.Layout.MainLayout>();
            Assert.Empty(cut.Instance.hostToTrace);
            httpContextAccessor = ContextAccessorHelper.GetContext("/", "localhost");
        }

        [Fact]
        public void TestShowServerEntry()
        {
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();

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
            Assert.Equal("Country - City - http://localhost", result);

            // Null Country & City
            serverEntry = new ServerEntry
            {
                url = "http://localhost",
                Details = new IpDetails()
            };
            result = cut.Instance.ShowServerEntry(serverEntry);
            Assert.Equal("http://localhost", result);
        }

        [Fact]
        public void TestRefreshServerList()
        {
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();
            cut.Instance.selectedServerUrl = "not existing server";
            cut.Instance.serverList = new();

            cut.Instance.RefreshServerList();
            Assert.Contains("localhost", cut.Instance.selectedServerUrl.ToLower());

            // I test it again to check the normal result
            cut.Instance.serverList = new();
            cut.Instance.RefreshServerList();
            Assert.Contains("localhost", cut.Instance.selectedServerUrl.ToLower());
        }

        [Fact]
        public async Task TestBeginTraceRoute()
        {
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();
            cut.Instance.selectedServerUrl = "not existing server";
            cut.Instance.hostToTrace = "127.0.0.1";
            toastFired = false;

            // Invalid server selected
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);
            Assert.True(toastFired);
            toastFired = false;

            // Local server selected
            cut.Instance.selectedServerUrl = cut.Instance.serverList.First().url;
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);
            Assert.False(toastFired);
            cut.WaitForAssertion(() =>
            {
                Assert.NotNull(cut.Instance.traceResult);
                Assert.True(cut.Instance.traceResult.Hops.Count > 0);
            });
            Assert.True(cut.Instance.traceResult!.Hops.First().Details.IsBogonIP);
            // Assert.False(cut.Instance.traceResult!.Hops.Last().Details.IsBogonIP);

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
            Assert.False(toastFired);

            // I generate a trace error
            toastFired = false;
            cut.Instance.selectedServerUrl = cut.Instance.serverList.First().url;
            cut.Instance.hostToTrace = Guid.NewGuid().ToString();
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);
            Assert.True(toastFired);

            // I make sure I cannot get the trace hop details
            IpApiClient.BASE_URL = "aaaa";
            cut.Instance.hostToTrace = "8.8.8.8";
            memoryCache.Clear();    // I need to empty the memory cache
            await cut.InvokeAsync(cut.Instance.BeginTraceRoute);          
            Assert.Null(cut.Instance.traceResult.Hops.Where(x => x.HopAddress == "8.8.8.8").First().Details.Country);
            IpApiClient.BASE_URL = "http://ip-api.com";
        }

        [Fact]
        public async Task TestOnShowHopDetails()
        {
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();

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
            Assert.Equal(hop, cut.Instance.currentHop);
        }

        [Fact]
        public async Task TestOnShowIpDetails()
        {
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();

            // Null result
            cut.Instance.traceResult = null;
            await cut.InvokeAsync(() => cut.Instance.OnShowIpDetails("asdf"));

            // Empty result
            toastFired = false;
            cut.Instance.traceResult = new TraceResultViewModel();
            await cut.InvokeAsync(() => cut.Instance.OnShowIpDetails("asdf"));
            Assert.True(toastFired);

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
            Assert.False(toastFired);
            Assert.Equal(cut.Instance.traceResult.Hops.First(), cut.Instance.currentHop);
        }

        [Fact]
        public async Task TestShowServerDetails()
        {
            var cut = Render<TraceRoute.Components.Layout.MainLayout>();

            // Non existing server
            cut.Instance.currentHop = null;
            cut.Instance.selectedServerUrl = Guid.NewGuid().ToString();
            await cut.InvokeAsync(() => cut.Instance.ShowServerDetails());
            Assert.Null(cut.Instance.currentHop);

            // Existing server
            cut.Instance.selectedServerUrl = cut.Instance.serverList.First().url;
            await cut.InvokeAsync(() => cut.Instance.ShowServerDetails());
            Assert.NotNull(cut.Instance.currentHop);
            Assert.Equal(cut.Instance.selectedServerUrl, cut.Instance.currentHop.HopAddress);
        }

        [Fact]
        public async Task TestIpApiNotAvailable()
        {
            IpApiClient _ipApiClient = Services.GetRequiredService<IpApiClient>();
            IpApiClient.BASE_URL = "not.existing.domain";

            try
            {
                toastFired = false;
                var cut = Render<TraceRoute.Components.Layout.MainLayout>();

                // I check the interface rendered initially
                var versionCheck = cut.Markup.Contains("https://hub.docker.com/");
                Assert.True(versionCheck);

                // I check the alert shown up
                cut.WaitForAssertion(
                    () => Assert.True(toastFired),
                    timeout: TimeSpan.FromSeconds(15));
            }
            finally
            {
                IpApiClient.BASE_URL = "http://ip-api.com";
            }
        }

        [Fact]
        public async Task TestIpApiTimeout()
        {
            // First I create a timing out web server
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0);
            });
            var app = builder.Build();
            app.MapGet("/{**path}", async (HttpContext context) =>
            {
                Console.WriteLine($"Ricevuta: {context.Request.Path}");
                await Task.Delay(TimeSpan.FromSeconds(30));
            });
            await app.StartAsync();
            // Then I retrieve the local url
            var addresses = app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>();
            String address = addresses!.Addresses.Single();

            IpApiClient _ipApiClient = Services.GetRequiredService<IpApiClient>();
            IpApiClient.BASE_URL = address;

            try {
                toastFired = false;
                var cut = Render<TraceRoute.Components.Layout.MainLayout>();

                // I check the interface rendered
                var versionCheck = cut.Markup.Contains("https://hub.docker.com/");
                Assert.True(versionCheck);

                // I check the alert shown up
                cut.WaitForAssertion(
                    () => Assert.True(toastFired),
                    timeout: TimeSpan.FromSeconds(60));
            }
            finally
            {
                IpApiClient.BASE_URL = "http://ip-api.com";
            }
        }
    }
}
