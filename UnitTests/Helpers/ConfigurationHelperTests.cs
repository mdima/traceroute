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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TraceRoute.Helpers;

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
        }

        [TestMethod]
        public void GetSetServerID()
        {
            Assert.IsTrue(ConfigurationHelper.SetServerID());
            string serverID = ConfigurationHelper.GetServerID();
            Assert.AreNotEqual("", serverID);
        }

        [TestMethod]
        public void GetSetEnableRemoteTraces()
        {
            Assert.IsTrue(ConfigurationHelper.SetEnableRemoteTraces(true));
            Assert.IsTrue(ConfigurationHelper.GetEnableRemoteTraces());

            Assert.IsTrue(ConfigurationHelper.SetEnableRemoteTraces(false));
            Assert.IsFalse(ConfigurationHelper.GetEnableRemoteTraces());
        }

        [TestMethod]
        public void GetSetHostRemoteTraces()
        {
            Assert.IsTrue(ConfigurationHelper.SetHostRemoteTraces(true));
            Assert.IsTrue(ConfigurationHelper.GetHostRemoteTraces());

            Assert.IsTrue(ConfigurationHelper.SetHostRemoteTraces(false));
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
        }
    }
}
