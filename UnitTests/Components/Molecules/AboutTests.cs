using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Helpers;
using TraceRoute.Services;

namespace UnitTests.Components.Molecules
{

    public class AboutTests : BunitContext
    {
        ServerListService _serverListService;

        public AboutTests()
        {
            NullLoggerFactory factory = new();

            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            ReverseLookupService reverseLookupService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);

            IpApiClient _ipApiClient = new(httpClient, factory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
            TraceRouteApiClient _traceRouteApiClient = new(httpClient, factory.CreateLogger<TraceRouteApiClient>());

            StoreServerURLFilter _storeServerURLFilter = new();
            IHttpContextAccessor _httpContextAccessor = ContextAccessorHelper.GetContext("/", "localhost", "127.0.0.1");
            Services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);

            _serverListService = new(factory.CreateLogger<ServerListService>(), _ipApiClient, _storeServerURLFilter, _traceRouteApiClient);
            Services.AddSingleton<ServerListService>(_serverListService);
        }

        [Fact]
        public void TestAbout()
        {
            // Arrange a simple render
            var cut = Render<TraceRoute.Components.Molecules.About>();
            Assert.NotNull(cut);
            // Check if the header contains the expected text
            Assert.Contains(cut.Instance.currentVersion!.Major + ".", cut.Markup);

            // Null assembly
            System.Reflection.Assembly.SetEntryAssembly(null);
            cut = Render<TraceRoute.Components.Molecules.About>();
            Assert.Contains("<span>Unknown</span>", cut.Markup);

            // I set the current version to null
            cut.Instance.currentVersion = null;
            cut.Render();
            Assert.Contains("<span>Unknown</span>", cut.Markup);
        }

        [Fact]
        public async Task TestCheckVersion()
        {
            // No new version available
            _serverListService._newVersionAvailable = false;
            var cut = Render<TraceRoute.Components.Molecules.About>();
            var versionCheck = cut.Find(".text-success");
            Assert.NotNull(versionCheck);

            // New version available
            await _serverListService.InitializePresence();
            _serverListService.localServer!.version = "not existing version";
            await _serverListService.SendPresenceToMainHost();
            cut.Render();
            versionCheck = cut.Find(".text-danger");
            Assert.NotNull(versionCheck);
        }
    }
}
