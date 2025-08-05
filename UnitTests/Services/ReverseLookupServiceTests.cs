using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Services
{

    public class ReverseLookupServiceTests
    {
        private ReverseLookupService _reverseService;

        public ReverseLookupServiceTests() {
            NullLoggerFactory factory = new();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });

            _reverseService = new(factory.CreateLogger<ReverseLookupService>(), memoryCache);
        }

        /// <summary>
        ///     This still cannot work because of this issue:
        ///     https://github.com/docker/for-win/issues/13681.
        ///     To make it work set the DNS to 8.8.8.8 in the global Docker settings.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetHostName()
        {
            string result = await _reverseService.GetHostName("192.188.248.215");

            Assert.NotNull(result);
            Assert.Equal("mail.nt2.it", result);        

            // Good case
            IPAddress iPAddress = IPAddress.Parse("192.188.248.215");
            result = await _reverseService.GetHostNameWindows(iPAddress);
            Assert.NotNull(result);
            Assert.Equal("mail.nt2.it", result);

            // Bad case
            result = await _reverseService.GetHostNameWindows(IPAddress.None);
            Assert.Empty(result);

            // Bad case 2
            result = await _reverseService.GetHostNameWindows(IPAddress.Any);
            Assert.Empty(result);

            // On Linux
            result = await _reverseService.GetHostNameLinux(iPAddress);
            Assert.NotNull(result);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal("", result);
            }
            else
            {
                Assert.Equal("mail.nt2.it", result);
            }            
        }

        [Fact]
        public async Task GetHostNameNull()
        {
            //Just test a random IP
            string result = await _reverseService.GetHostName("123.32.321.1");

            Assert.NotNull(result);
            Assert.Equal("", result);

            result = await _reverseService.GetHostName(null!);
            Assert.NotNull(result);
            Assert.Equal("", result);

        }
    }
}
