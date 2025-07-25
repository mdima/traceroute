﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TraceRoute.Helpers;
using TraceRoute.Models;

namespace UnitTests.Helpers
{
    [TestClass]
    public class ConfigurationHelperTests
    {
        [TestMethod]
        public void GetCacheMinutes()
        {
            Assert.AreEqual(60, ConfigurationHelper.GetCacheMinutes());
        }

        [TestMethod]
        public void GetRootNode()
        {
            Assert.AreEqual("https://traceroute.di-maria.it/", ConfigurationHelper.GetRootNode());
        }

        [TestMethod]
        public void IsRootNode()
        {
            Mock<HttpRequest> request = new Mock<HttpRequest>();
            request.Setup(r => r.Host).Returns(new HostString("traceroute.di-maria.it", 443));
            Assert.IsTrue(ConfigurationHelper.IsRootNode(request.Object));

            request.Setup(r => r.Host).Returns(new HostString("traceroute.di-maria.it", 442));
            Assert.IsFalse(ConfigurationHelper.IsRootNode(request.Object));

            request.Setup(r => r.Host).Returns(new HostString("test", 443));
            Assert.IsFalse(ConfigurationHelper.IsRootNode(request.Object));

            request.Setup(r => r.Host).Returns(new HostString("test", 442));
            Assert.IsFalse(ConfigurationHelper.IsRootNode(request.Object));

        }

        [TestMethod]
        public void GetCurrentSettings()
        {
            Mock<HttpRequest> request = new Mock<HttpRequest>();
            request.Setup(r => r.Host).Returns(new HostString("https://traceroute.di-maria.it/", 443));

            SettingsViewModel result = ConfigurationHelper.GetCurrentSettings(request.Object);

            Assert.AreEqual("https://traceroute.di-maria.it/:443", result.CurrentServerURL);            
            Assert.AreEqual(ConfigurationHelper.GetEnableRemoteTraces(), result.EnableRemoteTraces);
            Assert.AreEqual(ConfigurationHelper.GetHostRemoteTraces(), result.HostRemoteTraces);

            // Test with a null request
            result = ConfigurationHelper.GetCurrentSettings(null!);
            Assert.IsNotNull(result.CurrentServerURL);
        }

        [TestMethod]
        public void GetSetEnableRemoteTraces()
        {            
            Assert.IsTrue(ConfigurationHelper.GetEnableRemoteTraces());

            Environment.SetEnvironmentVariable("TRACEROUTE_ENABLEREMOTETRACES", "false");
            Assert.IsFalse(ConfigurationHelper.GetEnableRemoteTraces());
        }

        [TestMethod]
        public void GetSetHostRemoteTraces()
        {            
            Assert.IsTrue(ConfigurationHelper.GetHostRemoteTraces());

            Environment.SetEnvironmentVariable("TRACEROUTE_HOSTREMOTETRACES", "false");
            Assert.IsFalse(ConfigurationHelper.GetHostRemoteTraces());
        }

        [TestMethod]
        public void GetNumericValue()
        {
            var method = typeof(ConfigurationHelper).GetMethod("GetNumericValue", BindingFlags.Static | BindingFlags.NonPublic)!;
            object[] parameterValues =
            {
                "aaaa", 991
            };            
            int? result = (int?)method.Invoke(null, parameterValues);
            Assert.AreEqual(991, result);

            parameterValues = new object[]
            {
                "AppSettings:CacheMinutes", 100
            };
            result = (int?)method.Invoke(null, parameterValues);

            Assert.AreEqual(60, result);
        }

        [TestMethod]
        public void GetNumericValue_NoDefault()
        {
            var method = typeof(ConfigurationHelper).GetMethod("GetNumericValue", BindingFlags.Static | BindingFlags.NonPublic)!;
            object?[] parameterValues =
            {
                "aaaa",
                null
            };
            int? result = (int?)method.Invoke(null, parameterValues);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetAppSetting()
        {
            var method = typeof(ConfigurationHelper).GetMethod("GetAppSetting", BindingFlags.Static | BindingFlags.NonPublic)!;
            object[] parameterValues =
            {
                "RootNode", "https://traceroute.di-maria.it/"
            };
            string? result = (string?)method.Invoke(null, parameterValues);
            Assert.IsNotNull(result);
            Assert.AreEqual("https://traceroute.di-maria.it/", result);

            parameterValues = new object[]
            {
                "RootNodeNotExisting", "1234"
            };
            result = (string?)method.Invoke(null, parameterValues);
            Assert.IsNotNull(result);
            Assert.AreEqual("1234", result);
        }
    }
}
