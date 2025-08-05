using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

namespace UnitTests.Controllers
{

    public class APIControllerTests : Bunit.TestContext, IClassFixture<TracerouteWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly ServerListService _serverListService;

        public APIControllerTests(TracerouteWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
            _serverListService = factory.Services.GetRequiredService<ServerListService>();
        }

        [Fact]
        public async Task TraceRouteOK()
        {
            // First test
            HttpResponseMessage? clientResponse = await _client.GetAsync("/api/trace/192.188.248.215");
            clientResponse.EnsureSuccessStatusCode();

            TraceResultViewModel? response = await clientResponse.Content.ReadFromJsonAsync<TraceResultViewModel>();

            Assert.NotNull(response);
            Assert.True(response.Hops.Count >= 2);

            // Trace to localhost
            clientResponse = await _client.GetAsync("/api/trace/127.0.0.1");
            clientResponse.EnsureSuccessStatusCode();
            response = await clientResponse.Content.ReadFromJsonAsync<TraceResultViewModel>();

            Assert.NotNull(response);
            Assert.Empty(response.ErrorDescription);
            Assert.True(response.Hops.Count >= 1);

            TraceHop hop = response.Hops.First();
            Assert.Equal("127.0.0.1", hop.HopAddress);
        }

        [Fact]
        public async Task SecurityChecks()
        {
            HttpResponseMessage? clientResponse = await _client.GetAsync("/api/trace/www.nt2.it;ls/");
            clientResponse.EnsureSuccessStatusCode();

            TraceResultViewModel? response = await clientResponse.Content.ReadFromJsonAsync<TraceResultViewModel>();
            Assert.NotNull(response);
            Assert.NotEmpty(response.ErrorDescription);
        }

        [Fact]
        public async Task ReceivePresence()
        {
            // Negative case
            ServerEntry server = new();
            server.url = ConfigurationHelper.GetRootNode();

            // var stringData = new StringContent(JsonSerializer.Serialize(server), Encoding.UTF8, @"application/json");

            HttpResponseMessage? clientResponse = await _client.PostAsJsonAsync("/api/presence", server);
            clientResponse.EnsureSuccessStatusCode();

            bool result = await clientResponse.Content.ReadFromJsonAsync<bool>();
            Assert.False(result);

            // Positive case
            ServerEntry? rootServer = await _serverListService.GetRemoteServerInfo(server);
            Assert.NotNull(rootServer);
            Assert.Equal(server.url, rootServer.url);

            clientResponse = await _client.PostAsJsonAsync<ServerEntry>("/api/presence", rootServer);
            clientResponse.EnsureSuccessStatusCode();
            result = await clientResponse.Content.ReadFromJsonAsync<bool>();
            Assert.True(result);

            // null case
            clientResponse = await _client.PostAsJsonAsync<ServerEntry>("/api/presence", new ServerEntry());
            clientResponse.EnsureSuccessStatusCode();

            result = await clientResponse.Content.ReadFromJsonAsync<bool>();
            Assert.False(result);
        }

        [Fact]
        public async Task GetServerList()
        {
            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            ServerEntry? server = _serverListService.GetCurrentServerInfo();
            Assert.NotNull(server);

            HttpResponseMessage? clientResponse = await _client.GetAsync("/api/serverlist");
            clientResponse.EnsureSuccessStatusCode();

            List<ServerEntry>? serverList = await clientResponse.Content.ReadFromJsonAsync<List<ServerEntry>>();
            Assert.NotNull(serverList);

            Assert.Contains(serverList, x => x.url == server.url);
        }

        [Fact]
        public async Task GetServerInfo()
        {
            await ((IHostedService)_serverListService).StartAsync(new CancellationToken());
            ServerEntry? server = _serverListService.GetCurrentServerInfo();
            Assert.NotNull(server);

            HttpResponseMessage? clientResponse = await _client.GetAsync("/api/serverInfo");
            clientResponse.EnsureSuccessStatusCode();

            ServerEntry? serverInfo = await clientResponse.Content.ReadFromJsonAsync<ServerEntry>();
            Assert.NotNull(serverInfo);

            Assert.Equal(server.lastUpdate, serverInfo.lastUpdate);
        }
    }
}
