﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Services
{
    [TestClass]
    public class TraceRouteApiClientTests
    {
        TraceRouteApiClient _traceRouteApiClient;

        public TraceRouteApiClientTests() { 

            HttpClient httpClient = new HttpClient();
            ILogger<TraceRouteApiClient> logger = new LoggerFactory().CreateLogger<TraceRouteApiClient>();

            _traceRouteApiClient = new TraceRouteApiClient(httpClient, logger);
        }

        [TestMethod]
        public async Task TestSendPresence()
        {
            // Normal case
            ServerEntry localServer = new ServerEntry
            {
                isLocalHost = true,
                url = "http://localhost",
                Details = new()
            };

            bool result = await _traceRouteApiClient.SendPresence(localServer, CancellationToken.None);
            Assert.IsFalse(result); // This is false because the remote host cannot check the localhost node

            // I expect an error from the root node
            _traceRouteApiClient.rootNodeBaseAddress = "https://traceroute.di-maria.it/test";
            result = await _traceRouteApiClient.SendPresence(localServer, CancellationToken.None);
            Assert.IsFalse(result);

            // I cause an exception in the HttpClient
            _traceRouteApiClient.rootNodeBaseAddress = "asdf";
            result = await _traceRouteApiClient.SendPresence(localServer, CancellationToken.None);
            Assert.IsFalse(result);

            // Finally I rese the rootNodeBaseAddress
            _traceRouteApiClient.rootNodeBaseAddress = "https://traceroute.di-maria.it/";
        }

        [TestMethod]
        public async Task TestGetServerList()
        {
            List<ServerEntry>? result = await _traceRouteApiClient.GetServerList(CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Count >= 1);
            Assert.IsNotNull(result.Where(x => x.url == _traceRouteApiClient.rootNodeBaseAddress).FirstOrDefault());

            // I expect an error from the root node
            _traceRouteApiClient.rootNodeBaseAddress = "https://traceroute.di-maria.it/test";
            result = await _traceRouteApiClient.GetServerList(CancellationToken.None);
            Assert.IsNull(result);

            // I cause an exception in the HttpClient
            _traceRouteApiClient.rootNodeBaseAddress = "asdf";
            result = await _traceRouteApiClient.GetServerList(CancellationToken.None);
            Assert.IsNull(result);

            // Finally I rese the rootNodeBaseAddress
            _traceRouteApiClient.rootNodeBaseAddress = "https://traceroute.di-maria.it/";
        }

        [TestMethod]
        public async Task TestGetServerInfo()
        {
            // Normal case
            ServerEntry localServer = new ServerEntry
            {
                isLocalHost = true,
                url = ConfigurationHelper.GetRootNode(),
                Details = new()
            };

            ServerEntry? result = await _traceRouteApiClient.GetServerInfo(localServer);
            Assert.IsNotNull(result);
            Assert.AreEqual(localServer.url, result.url);

            // I expect an error from the root node
            localServer.url = "https://traceroute.di-maria.it/test";
            result = await _traceRouteApiClient.GetServerInfo(localServer);
            Assert.IsNull(result);

            // I cause an exception in the HttpClient
            localServer.url = "asdf";
            result = await _traceRouteApiClient.GetServerInfo(localServer);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestRemoteTrace()
        {
            // Normal case
            TraceResultViewModel result = await _traceRouteApiClient.RemoteTrace("192.188.248.215", ConfigurationHelper.GetRootNode());
            Assert.IsNotNull(result);
            Assert.IsEmpty(result.ErrorDescription);
            Assert.IsTrue(result.Hops.Count > 2);

            // I expect an error from the root node
            result = await _traceRouteApiClient.RemoteTrace("192.188.248.215", ConfigurationHelper.GetRootNode() + "/asdf/");
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.ErrorDescription);
            Assert.IsEmpty(result.Hops);

            // I cause an exception in the HttpClient
            result = await _traceRouteApiClient.RemoteTrace("192.188.248.215", "notexistingserver");
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.ErrorDescription);
            Assert.IsEmpty(result.Hops);
        }
    }
}
