using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
            _controller.ControllerContext = new ControllerContext();
            _controller.ControllerContext.HttpContext = ContextAccessorHelper.GetContext("/", "http").HttpContext!;
            _controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = new System.Net.IPAddress(0x2414188C);
            _controller.ControllerContext.HttpContext.TraceIdentifier = "1234";
        }

        [TestMethod]
        public void Home()
        {
            IActionResult response = _controller.Index();
            Assert.IsNotNull(response);

            Assert.AreEqual(((ViewResult)response).ViewData["ClientIPAddress"], new System.Net.IPAddress(0x2414188C).ToString());
            Assert.IsNotNull(((ViewResult)response).ViewData["Version"]);
        }

        [TestMethod]
        public void Error()
        {
            IActionResult response = _controller.Error(500);

            Assert.IsNotNull(response);

            ErrorViewModel? model = (ErrorViewModel)((ViewResult)response).Model!;
            Assert.IsTrue(model.ShowRequestId);
        }
    }
}
