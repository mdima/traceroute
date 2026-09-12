using Bunit;
using Microsoft.AspNetCore.Components;
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
using TraceRoute.Components.Pages;
using TraceRoute.Models;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

namespace UnitTests.Components.Pages
{

    public class HomeTests : BunitContext
    {
        public HomeTests()
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

        [Fact]
        public void TestHome()
        {
            // Arrange a simple render with empty values
            var cut = Render<Home>();
            Assert.NotNull(cut);

            // I prepare some test data
            List<TraceHop> Hops= new()
            {
                new TraceHop
                {
                    HopAddress = "123",
                    Details = new IpDetails
                    {
                        IsBogonIP = true,
                    }
                },
                new TraceHop
                {
                    HopAddress = "a.b.c.d.",
                    Details = new IpDetails
                    {
                        IsBogonIP = false,
                        HostName = "hostname",
                        ISP = "ISP Name"
                    }
                },
                new TraceHop
                {
                    HopAddress = "...",
                    Details = new IpDetails
                    {
                        IsBogonIP = false,
                        ISP = "-"
                    }
                },
            };
            // I rerender the component with the test data
            cut = Render<Home>(p =>
            {
                p.Add(a => a.Hops, Hops);
            });

            // Assert that the component is rendered correctly
            Assert.NotNull(cut);
            Assert.Contains("a.b.c.d.", cut.Markup);
            Assert.Contains("ISP Name", cut.Markup);
            Assert.Contains("hostname", cut.Markup);

            // I test the details
            Boolean eventFired = false;
            cut = Render<Home>(p =>
            {
                p.Add(a => a.Hops, Hops);
                p.Add(p => p.OnShowHopDetails, EventCallback.Factory.Create<TraceHop>(this, (hop) =>
                 {
                     eventFired = true;
                 }));
            });
            cut.InvokeAsync(() => cut.Instance.IpDetails(Hops[0]));
            Assert.True(eventFired);

            // I run the method again using the click
            eventFired = false;
            cut.Find(".link-secondary").Click();
            Assert.True(eventFired);
        }
    }
}
