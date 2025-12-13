using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Services
{

    public class TraceRouteServiceTests
    {
        private TracerouteService _tracerouteService;

        public TraceRouteServiceTests()
        {
            NullLoggerFactory factory = new();
            BogonIPService bogonIPService = new(factory);
            _tracerouteService = new(bogonIPService, factory.CreateLogger<TracerouteService>());
        }

        [Fact]
        public async Task TestTraceRoute()
        {
            // Arrange
            string destination = "www.google.com";
            // Act
            List<string> result = await _tracerouteService.TraceRoute(destination);
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            // Windows
            result = _tracerouteService.TraceRouteWindows(destination).ToList();
            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            // Linux
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                result = await _tracerouteService.TraceRouteLinux(destination);
                Assert.NotNull(result);
                Assert.True(result.Count > 0);
            }

            // Unreachanble destination
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            { 
                destination = "www.microsoft.com";
                result = await _tracerouteService.TraceRoute(destination);
                // Assert
                Assert.NotNull(result);
                Assert.True(result.Count > 0);
                Assert.Contains("x ... 0", result);
            }
        }

        [Fact]
        public async Task TestTraceRouteFull()
        {
            // Arrange
            string destination = "www.google.com";

            // Act
            TraceResultViewModel? result = await _tracerouteService.TraceRouteFull(destination);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Hops.Count > 0);

            // Bad cases
            result = await _tracerouteService.TraceRouteFull("wrongname");
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Hops.Count == 0);
        }

        [Fact]
        public async Task TestEnsureIpAddress()
        {
            // Arrange
            string destination = "www.google.com";
            // Act
            string? result = await _tracerouteService.EnsureIpAddress(destination);
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            result = await _tracerouteService.EnsureIpAddress("mail.nt2.it");            
            Assert.NotNull(result);
            Assert.Equal("192.188.248.215", result);

            result = await _tracerouteService.EnsureIpAddress("192.188.248.218");
            Assert.NotNull(result);
            Assert.Equal("192.188.248.218", result);

            // Bad cases
            result = await _tracerouteService.EnsureIpAddress("wrongname");
            // Assert
            Assert.NotNull(result);
            Assert.Equal("wrongname", result);
        }

    }
}
