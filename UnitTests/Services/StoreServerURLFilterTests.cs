using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Services;

namespace UnitTests.Services
{


    public class StoreServerURLFilterTests
    {
        [Fact]
        public async Task TestFilter()
        {
            StoreServerURLFilter filter = new();
            StoreServerURLFilter.ServerURL = "";

            // Empty initial state
            IHttpContextAccessor contextAccessor = ContextAccessorHelper.GetContext("", "");
            await filter.InvokeAsync(contextAccessor.HttpContext!, async (context) => 
            {
                // Simulate a request
                context.Request.Scheme = "http";
                context.Request.Host = new HostString("localhost", 5000);
                await Task.CompletedTask;
            });
            Assert.Empty(filter.GetServerURL());

            // Good request
            contextAccessor = ContextAccessorHelper.GetContext("/", "localhost", "127.0.0.1");

            await filter.InvokeAsync(contextAccessor.HttpContext!, async (context) =>
            {
                // Simulate a request
                context.Request.Scheme = "http";
                context.Request.Host = new HostString("localhost", 80);
                await Task.CompletedTask;
            });
            Assert.NotEmpty(filter.GetServerURL());
            Assert.Equal("https://localhost/", filter.GetServerURL());
        }
    }
}
