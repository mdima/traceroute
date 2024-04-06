using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Services;
using TraceRoute;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;

namespace UnitTests
{
    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public void Program()
        {
            var waf = new WebApplicationFactory<Program>();//                .WithWebHostBuilder(builder => builder.UseSolutionRelativeContentRoot("/"));
            var server = waf.Server;
            Assert.IsNotNull(server);

            // Check for individual services
            var scope = server.Services.CreateScope();

            var ipApiClient = scope.ServiceProvider.GetService<IpApiClient>();
            Assert.IsNotNull(ipApiClient);

            var memoryCache = scope.ServiceProvider.GetService<IMemoryCache>();
            Assert.IsNotNull(memoryCache);
        }
    }
}