using System;
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
using Moq;
using TraceRoute.Helpers;
using TraceRoute.Models;

namespace UnitTests.Helpers
{

    public class ConfigurationHelperTests
    {
        [Fact]
        public void GetCacheMinutes()
        {
            Assert.Equal(60, ConfigurationHelper.GetCacheMinutes());
        }

        [Fact]
        public void GetRootNode()
        {
            Assert.Equal("https://traceroute.di-maria.it/", ConfigurationHelper.GetRootNode());
        }

        [Fact]
        public void IsRootNode()
        {
            Mock<HttpRequest> request = new Mock<HttpRequest>();
            request.Setup(r => r.Host).Returns(new HostString("traceroute.di-maria.it", 443));
            Assert.True(ConfigurationHelper.IsRootNode(request.Object));

            request.Setup(r => r.Host).Returns(new HostString("traceroute.di-maria.it", 442));
            Assert.False(ConfigurationHelper.IsRootNode(request.Object));

            request.Setup(r => r.Host).Returns(new HostString("test", 443));
            Assert.False(ConfigurationHelper.IsRootNode(request.Object));

            request.Setup(r => r.Host).Returns(new HostString("test", 442));
            Assert.False(ConfigurationHelper.IsRootNode(request.Object));

        }

        [Fact]
        public void GetCurrentSettings()
        {
            Mock<HttpRequest> request = new Mock<HttpRequest>();
            request.Setup(r => r.Host).Returns(new HostString("https://traceroute.di-maria.it/", 443));

            SettingsViewModel result = ConfigurationHelper.GetCurrentSettings(request.Object);

            Assert.Equal("https://traceroute.di-maria.it/:443", result.CurrentServerURL);            
            Assert.Equal(ConfigurationHelper.GetEnableRemoteTraces(), result.EnableRemoteTraces);
            Assert.Equal(ConfigurationHelper.GetHostRemoteTraces(), result.HostRemoteTraces);

            // Test with a null request
            result = ConfigurationHelper.GetCurrentSettings(null!);
            Assert.NotNull(result.CurrentServerURL);
        }

        [Fact]
        public void GetSetEnableRemoteTraces()
        {            
            Assert.True(ConfigurationHelper.GetEnableRemoteTraces());

            Environment.SetEnvironmentVariable("TRACEROUTE_ENABLEREMOTETRACES", "false");
            Assert.False(ConfigurationHelper.GetEnableRemoteTraces());
        }

        [Fact]
        public void GetSetHostRemoteTraces()
        {            
            Assert.True(ConfigurationHelper.GetHostRemoteTraces());

            Environment.SetEnvironmentVariable("TRACEROUTE_HOSTREMOTETRACES", "false");
            Assert.False(ConfigurationHelper.GetHostRemoteTraces());
        }

        [Fact]
        public void GetNumericValue()
        {
            var method = typeof(ConfigurationHelper).GetMethod("GetNumericValue", BindingFlags.Static | BindingFlags.NonPublic)!;
            object[] parameterValues =
            {
                "aaaa", 991
            };            
            int? result = (int?)method.Invoke(null, parameterValues);
            Assert.Equal(991, result);

            parameterValues = new object[]
            {
                "AppSettings:CacheMinutes", 100
            };
            result = (int?)method.Invoke(null, parameterValues);

            Assert.Equal(60, result);
        }

        [Fact]
        public void GetNumericValue_NoDefault()
        {
            var method = typeof(ConfigurationHelper).GetMethod("GetNumericValue", BindingFlags.Static | BindingFlags.NonPublic)!;
            object?[] parameterValues =
            {
                "aaaa",
                null
            };
            int? result = (int?)method.Invoke(null, parameterValues);

            Assert.Null(result);
        }

        [Fact]
        public void GetAppSetting()
        {
            var method = typeof(ConfigurationHelper).GetMethod("GetAppSetting", BindingFlags.Static | BindingFlags.NonPublic)!;
            object[] parameterValues =
            {
                "RootNode", "https://traceroute.di-maria.it/"
            };
            string? result = (string?)method.Invoke(null, parameterValues);
            Assert.NotNull(result);
            Assert.Equal("https://traceroute.di-maria.it/", result);

            parameterValues = new object[]
            {
                "RootNodeNotExisting", "1234"
            };
            result = (string?)method.Invoke(null, parameterValues);
            Assert.NotNull(result);
            Assert.Equal("1234", result);
        }
    }
}
