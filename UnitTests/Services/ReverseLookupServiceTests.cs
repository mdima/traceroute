﻿using Microsoft.Extensions.Caching.Memory;
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
    [TestClass]
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
        [TestMethod]
        public async Task GetHostName()
        {
            string result = await _reverseService.GetHostName("192.188.248.215");

            Assert.IsNotNull(result);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.AreEqual("mail.nt2.it", result);
            }
            else
            {
                Assert.AreEqual("mail.nt2.it.", result);
            }            

            // Good case
            IPAddress iPAddress = IPAddress.Parse("192.188.248.215");
            result = await _reverseService.GetHostNameWindows(iPAddress);
            Assert.IsNotNull(result);
            Assert.AreEqual("mail.nt2.it", result);

            // Bad case
            result = await _reverseService.GetHostNameWindows(IPAddress.None);
            Assert.IsEmpty(result);

            // Bad case 2
            result = await _reverseService.GetHostNameWindows(IPAddress.Any);
            Assert.IsEmpty(result);

            // On Linux
            result = await _reverseService.GetHostNameLinux(iPAddress);
            Assert.IsNotNull(result);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.AreEqual("", result);
            }
            else
            {
                Assert.AreEqual("mail.nt2.it.", result);
            }            
        }

        [TestMethod]
        public async Task GetHostNameNull()
        {
            //Just test a random IP
            string result = await _reverseService.GetHostName("123.32.321.1");

            Assert.IsNotNull(result);
            Assert.AreEqual("", result);

            result = await _reverseService.GetHostName(null!);
            Assert.IsNotNull(result);
            Assert.AreEqual("", result);

        }
    }
}
