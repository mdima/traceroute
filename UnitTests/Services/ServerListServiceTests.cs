﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;

namespace UnitTests.Services
{
    [TestClass]
    public class ServerListServiceTests : Bunit.TestContext
    {
        private IpApiClient _ipApiClient;
        private NullLoggerFactory _loggingFactory;
        private StoreServerURLFilter _storeServerURLFilter;
        private IHttpContextAccessor _httpContextAccessor;
        private TraceRouteApiClient _traceRouteApiClient;
        private ServerListService _serverListService;

        public ServerListServiceTests()
        {
            _loggingFactory = new();
            HttpClient httpClient = new HttpClient();
            MemoryCache memoryCache = new(new MemoryCacheOptions() { TrackStatistics = true, TrackLinkedCacheEntries = true });
            ReverseLookupService reverseLookupService = new(_loggingFactory.CreateLogger<ReverseLookupService>(), memoryCache);

            _ipApiClient = new(httpClient, _loggingFactory.CreateLogger<IpApiClient>(), memoryCache, reverseLookupService);
            _traceRouteApiClient = new(httpClient, _loggingFactory.CreateLogger<TraceRouteApiClient>());

            BogonIPService bogonIPService = new(_loggingFactory);

            _storeServerURLFilter = new();
            _httpContextAccessor = ContextAccessorHelper.GetContext("/", "localhost", "127.0.0.1");
            Services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);

            _serverListService = new(_loggingFactory.CreateLogger<ServerListService>(), _ipApiClient, _storeServerURLFilter, _traceRouteApiClient);
            Services.AddSingleton<ServerListService>(_serverListService);
            Services.AddHostedService(provider => provider.GetRequiredService<ServerListService>());

            IpApiClient.BASE_URL = "http://ip-api.com";
        }

        [TestMethod("Can start and stop the service")]
        public async Task StartAndStopAsync()
        {            

            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            
            // Assert first result
            List<ServerEntry> result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].isLocalHost);
            Assert.AreEqual("Localhost", result[0].url);

            // I set the server URL
            Assert.IsNotNull(_httpContextAccessor.HttpContext);
            ActionContext actionContext = new(_httpContextAccessor.HttpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            ActionExecutingContext actionExecutingContext = new(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), _serverListService);

            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            ServerEntry? serverEntry = result.Where(x => x.isLocalHost).FirstOrDefault();
            Assert.AreEqual("Localhost", result[0].url);

            ServerEntry? serverEntryLocal = _serverListService.GetCurrentServerInfo();
            Assert.IsNotNull(serverEntryLocal);
            Assert.AreSame(serverEntry, serverEntryLocal);

            // I force an error to simulate a bad response from the IPApiClient
            IpApiClient.BASE_URL = "asdf";
            StoreServerURLFilter.ServerURL = "http://localhost:5000";
            Thread.Sleep(4000);
            Assert.IsNotNull(result);
            serverEntry = result.Where(x => x.isLocalHost).FirstOrDefault();
            Assert.AreEqual("Localhost", result[0].url);

            // I set the server URL to the root node
            IpApiClient.BASE_URL = "http://ip-api.com";
            StoreServerURLFilter.ServerURL = ConfigurationHelper.GetRootNode();
            Thread.Sleep(4000);
            result = _serverListService.GetServerList();
            Assert.IsTrue(result.Count >= 1);

            // I force the server URL and wait for 5 seconds to attempt a retry
            IpApiClient.BASE_URL = "http://ip-api.com";
            StoreServerURLFilter.ServerURL = "http://localhost:5000";
            _serverListService._serverList.Clear();
            await _serverListService.InitializePresence();
            result = _serverListService.GetServerList();
            Assert.IsTrue(result.Count >= 1);    // I assume the root node is running

            // I stop the service
            await ((IHostedService)_serverListService).StopAsync(new CancellationToken());
            result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // I stop the service again with a valid cancellationToken and an active _timerPresence timer
            var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            Assert.IsTrue(token.CanBeCanceled);
            ServerListService._timerPresence = new Timer(async _ =>
            {
                await _serverListService.InitializePresence();
            }, null, 30000, Timeout.Infinite);
            ServerListService._cancelCurrentOperation = cts.Token;

            await ((IHostedService)_serverListService).StopAsync(cts.Token);
            result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod("Can clean the server list")]
        public async Task CleanServerList()
        {
            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());            
            List<ServerEntry> result = _serverListService.GetServerList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count >= 1);

            // I add an expired server
            ServerEntry serverEntry = new()
            {
                lastUpdate = DateTime.Now.AddMinutes(-10),
                url = "http://localhost:5000",
                isLocalHost = false
            };
            ServerEntry serverEntry2 = new()
            {
                lastUpdate = DateTime.Now,
                url = "http://localhost:5001",
                isLocalHost = false
            };
            _serverListService._serverList.Add(serverEntry);
            _serverListService._serverList.Add(serverEntry2);
            List<ServerEntry> secondResult = _serverListService.GetServerList();

            Assert.IsTrue(secondResult.Count == result.Count + 2);

            // I clean the server list
            _serverListService.CleanServerList();
            Assert.AreEqual(2, _serverListService.GetServerList().Count);

            // I check if the expired server is no longer there
            List<ServerEntry> thirdResult = _serverListService.GetServerList();
            Assert.IsEmpty(thirdResult.Where(x => x.url == "http://localhost:5000"));
        }

        [TestMethod]
        public async Task SendPresenceToMainHost()
        {
            // Good result
            await _serverListService.InitializePresence();
            await _serverListService.SendPresenceToMainHost();

            List<ServerEntry> result = _serverListService.GetServerList();
            Assert.IsNotNull(result.Where(x => x.url == ConfigurationHelper.GetRootNode()).FirstOrDefault());

            // Check the new version available or not
            ServerEntry? rootNode = result.Where(x => x.url == ConfigurationHelper.GetRootNode()).FirstOrDefault();
            Assert.IsNotNull(rootNode);
            Assert.IsNotNull(_serverListService.localServer);
            _serverListService.localServer.version = rootNode.version;
            await _serverListService.SendPresenceToMainHost();
            Assert.IsFalse(_serverListService.IsNewVersionAvailable());

            _serverListService.localServer.version = "old version";
            await _serverListService.SendPresenceToMainHost();
            Assert.IsTrue(_serverListService.IsNewVersionAvailable());
            Assert.AreEqual(rootNode.version, _serverListService.GetRootNodeVersion());

            // I make sure that the service cannot update the server list
            _traceRouteApiClient.rootNodeBaseAddress = "http://localhost:5000";
            await _serverListService.SendPresenceToMainHost();
            List<ServerEntry> result2 = _serverListService.GetServerList();
            _traceRouteApiClient.rootNodeBaseAddress = ConfigurationHelper.GetRootNode();
            Assert.AreEqual(result.Count, result2.Count);
        }

        [TestMethod]
        public void AddServer()
        {
            ServerEntry serverEntry = new()
            {
                lastUpdate = DateTime.Now.AddMinutes(-10),
                url = "http://localhost:5014"
            };
            Assert.IsTrue(_serverListService.AddServer(serverEntry));
            List<ServerEntry> result = _serverListService.GetServerList();
            Assert.IsNotNull(result.Where(x => x.url == serverEntry.url).FirstOrDefault());

            // I try to add the same server again
            Assert.IsFalse(_serverListService.AddServer(serverEntry));
            result = _serverListService.GetServerList();
            Assert.AreEqual(1, result.Where(x => x.url == serverEntry.url).Count());
        }

        [TestMethod]
        public async Task GetRemoteServerInfo()
        {
            ServerEntry serverEntry = new()
            {
                lastUpdate = DateTime.Now.AddMinutes(-10),
                url = "http://localhost:5014"
            };
            Assert.IsNull(await _serverListService.GetRemoteServerInfo(serverEntry));

            serverEntry.url = ConfigurationHelper.GetRootNode();
            ServerEntry? result = await _serverListService.GetRemoteServerInfo(serverEntry);
            Assert.IsNotNull(result);
            Assert.AreEqual(serverEntry.url, result.url);
        }
    }
}
