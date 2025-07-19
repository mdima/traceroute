using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Components.Molecules;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

namespace UnitTests.Components.Molecules
{
    [TestClass]
    public class SettingTests : Bunit.TestContext
    {
        public SettingTests()
        {
            // Initialize any required services or components here
            Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Initialize the test context for Bunit
            NullLoggerFactory loggingFactory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            ReverseLookupService reverseLookupService = new(loggingFactory.CreateLogger<ReverseLookupService>(), memoryCache);
            Services.AddSingleton(reverseLookupService);

            IpApiClient ipApiClient = new(httpClient, loggingFactory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
            Services.AddHttpClient<IpApiClient>();

            Services.AddMemoryCache(x => { x.TrackStatistics = true; x.TrackLinkedCacheEntries = true; });
        }

        [TestMethod]
        public void TestSettings()
        {
            // Arrange a simple render with empty values
            var cut = RenderComponent<Settings>();
            Assert.IsNotNull(cut);
            Assert.IsEmpty(cut.Instance.settings.ServerLocation);

            // I set the HttpContext to simulate a request
            ContextAccessorHelper.GetContext("/", "localhost");
            cut = RenderComponent<Settings>();
            cut.WaitForAssertion(() =>
            {
                Assert.IsNotNull(cut.Instance.settings);
                Assert.IsNotEmpty(cut.Instance.settings.ServerLocation);
            }, TimeSpan.FromSeconds(10));

            Assert.IsNotEmpty(cut.Instance.settings.ServerLocation);
        }
    }
}
