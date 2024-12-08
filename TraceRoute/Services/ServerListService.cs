using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using TraceRoute.Helpers;
using TraceRoute.Models;

namespace TraceRoute.Services
{
    /// <summary>
    /// Manages the list of servers that host the tracing service.
    /// </summary>
    /// <param name="logger">The Logger service</param>
    /// <param name="ipApiClient">The IpApiClient service</param>
    /// <param name="storeServerURLFilter">The StoreServerURLFilter filter</param>
    /// <param name="TraceRouteApiClient">The TraceRouteApiClient service</param>
    public class ServerListService(ILogger<ServerListService> logger, IpApiClient ipApiClient, StoreServerURLFilter storeServerURLFilter, TraceRouteApiClient TraceRouteApiClient) : IHostedService
    {
        private readonly ILogger _logger = logger;
        private readonly IpApiClient _ipApiClient = ipApiClient;
        private static Timer? _timerPresence = null;
        private static Timer? _timerServerList = null;
        private static CancellationToken _cancelCurrentOperation = new CancellationToken();
        private readonly StoreServerURLFilter _storeServerURLFilter = storeServerURLFilter;
        private readonly TraceRouteApiClient _traceRouteApiClient = TraceRouteApiClient;
        ServerEntry? localServer;
        private ConcurrentBag<ServerEntry> _serverList = new();

        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting the ServerListService");

            await InitializePresence();
        }

        private async Task InitializePresence()
        {
            if (_timerPresence != null) await _timerPresence.DisposeAsync();

            if (!String.IsNullOrEmpty(_storeServerURLFilter.getServerURL()))
            {                
                IpApiResponse? localHostInfo = await _ipApiClient.GetCurrentServerDetails();
                if (localHostInfo != null)
                {
                    localServer = new(localHostInfo)
                    {
                        lastUpdate = DateTime.Now,
                        isOnline = true,
                        isLocalHost = true,
                        query = "Localhost",
                        url = _storeServerURLFilter.getServerURL()
                    };
                    _serverList = [localServer];
                    _logger.LogInformation("Local server information initialized");
                    await SendPresenceToMainHost();
                }
                else
                {
                    _logger.LogError("Error getting the local server information");
                }                
            }
            else
            {
                // I add a dummy server just to receive calls as local server
                localServer = new()
                {
                    lastUpdate = DateTime.Now,
                    isOnline = true,
                    isLocalHost = true,
                    query = "Localhost"
                };
                _serverList = [localServer];

                _logger.LogInformation("The HttpContext last URL is not available");
            }

            //I schedule the next initialization if not initialized
            if (localServer == null || localServer.url == null)
            {
                _timerPresence = new Timer(async _ =>
                {
                    await InitializePresence();
                }, null, 10000, Timeout.Infinite);
            }
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping the ServerListService");

            _serverList = new();

            if (_timerPresence != null)
            {
                _timerPresence.Change(Timeout.Infinite, Timeout.Infinite);
                _timerPresence.Dispose();
                _timerPresence = null;
            }

            if (_cancelCurrentOperation.CanBeCanceled)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancelCurrentOperation);
                cancellationToken = cts.Token;
                cts.Cancel();
                _logger.LogDebug("Current operation cancelled");
            }            
            return Task.CompletedTask;
        }

        public List<ServerEntry> GetServerList()
        {
            return _serverList.ToList();
        }

        private async Task SendPresenceToMainHost()
        {
            _logger.LogDebug("Sending presence to the main host");
            if (_timerPresence != null) await _timerPresence.DisposeAsync();

            if (localServer != null && localServer.url != ConfigurationHelper.GetRootNode())
            {
                // I send the local server info to the root server
                await _traceRouteApiClient.SendPresence(localServer, _cancelCurrentOperation);
            }

            // I set the next execution cycle
            _timerPresence = new Timer(async _ =>
            {
                await SendPresenceToMainHost();
            }, null, 60000, Timeout.Infinite);
        }

        public bool AddServer(ServerEntry server)
        {
            if (_serverList.Where(x => x.url == server.url).Count() == 0)
            {
                server.isLocalHost = false;
                _serverList.Add(server);
                _serverList = new( _serverList.OrderBy(x => x.country).ThenBy(y => y.city).ToList());
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
