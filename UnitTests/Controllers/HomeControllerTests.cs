using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Controllers
{
    [TestClass]
    public class HomeControllerTests
    {
        private HomeController _controller;

        public HomeControllerTests()
        {
            NullLoggerFactory factory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            BogonIPService bogonIPService = new(factory);

            _controller = new(bogonIPService, factory);
        }

        [TestMethod]
        public void Home()
        {
            var response = _controller.Index();

            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void About()
        {
            var response = _controller.About();

            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void Error()
        {
            var response = _controller.Error();

            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void Settings()
        {
            var response = _controller.Settings();

            Assert.IsNotNull(response);
        }
    }
}
