using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Helpers;
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

        [TestMethod]
        public async Task TraceRoute()
        {
            // Arrange
            string destination = "www.google.com";
            // Act
            List<string> result = await _tracerouteService.TraceRoute(destination);
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);

            // Windows
            result = _tracerouteService.TraceRouteWindows(destination).ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);

            // Linux
            result = await _tracerouteService.TraceRouteLinux(destination);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

    }
}
