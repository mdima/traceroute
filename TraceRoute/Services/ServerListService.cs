﻿using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using TraceRoute.Helpers;
using TraceRoute.Models;

[assembly: InternalsVisibleTo("UnitTests")]
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
        internal ConcurrentBag<ServerEntry> _serverList = new();
        public Action? ServiceInitialized;

        /// <summary>
        /// Start the service
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting the ServerListService");

            _serverList = new();
            await InitializePresence();
        }

        internal async Task InitializePresence()
        {
            if (_timerPresence != null) await _timerPresence.DisposeAsync();

            if (!String.IsNullOrEmpty(_storeServerURLFilter.GetServerURL()))
            {                
                IpDetails? localHostInfo = await _ipApiClient.GetCurrentServerDetails();
                if (localHostInfo != null)
                {
                    localServer = new()
                    {
                        lastUpdate = DateTime.Now,
                        isOnline = true,
                        isLocalHost = true,                        
                        url = _storeServerURLFilter.GetServerURL(),
                        Details = localHostInfo,
                    };
                    _serverList = [localServer];
                    _logger.LogInformation("Local server information initialized");
                    ServiceInitialized?.Invoke();
                    if (localServer.url != ConfigurationHelper.GetRootNode())
                    {
                        // This is a client node
                        if (ConfigurationHelper.GetHostRemoteTraces())
                        {
                            await SendPresenceToMainHost();
                        }
                        else
                        {                            
                            _logger.LogDebug("Hosting of remote traces are disabled");
                        }
                    }
                    else
                    {
                        // This is the root node
                        if (ConfigurationHelper.GetEnableRemoteTraces())
                        {
                            // I start the clean server list function with its own timer
                            await CleanServerList();
                        }
                        else
                        {
                            _logger.LogDebug("Remote traces are disabled");
                        }
                    }
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
                    isOnline = false,
                    isLocalHost = true,
                    url = "Localhost"
                };
                _serverList = [localServer];

                _logger.LogInformation("The HttpContext last URL is not available");
            }

            //I schedule the next initialization if not initialized
            if (localServer == null || !localServer.isOnline)
            {
                _timerPresence = new Timer(async _ =>
                {
                    await InitializePresence();
                }, null, 3000, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Stops the service and cleans up resources.
        /// </summary>
        /// <param name="cancellationToken">The CancellationToken</param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the list of servers.
        /// </summary>
        /// <returns>The list of srevers</returns>
        public List<ServerEntry> GetServerList()
        {
            return _serverList.ToList();
        }

        /// <summary>
        /// Sends the presence of the current server to the root host and retrieves the server list from it.
        /// </summary>
        /// <returns></returns>
        internal async Task SendPresenceToMainHost()
        {
            _logger.LogDebug("Sending presence to the root host");
            if (_timerPresence != null) await _timerPresence.DisposeAsync();

            if (localServer != null)
            {
                // I send the local server info to the root server
                await _traceRouteApiClient.SendPresence(localServer, _cancelCurrentOperation);

                // I retrieve the server list from the root server
                _logger.LogDebug("Updating the server list from the root host");
                List<ServerEntry>? receivedServerList = await _traceRouteApiClient.GetServerList(_cancelCurrentOperation);
                if (receivedServerList != null)
                {
                    List<ServerEntry>? newServerList = new();
                    foreach (ServerEntry server in receivedServerList)
                    {
                        if (server.url != localServer.url)
                        {
                            server.isLocalHost = false;
                            newServerList.Add(server);
                        }
                    }
                    newServerList.Add(localServer);
                    _serverList = new(newServerList.OrderBy(x => x.Details.Country).ThenBy(y => y.Details.City).ToList());
                    _logger.LogInformation("Server list updated");
                }
                else
                {
                    _logger.LogError("Error updating the server list");
                }
            }

            // I set the next execution cycle
            _timerPresence = new Timer(async _ =>
            {
                await SendPresenceToMainHost();
            }, null, 60000, Timeout.Infinite);
        }

        /// <summary>
        /// Re initialize the server list by removing expired servers.
        /// </summary>
        /// <returns></returns>
        internal async Task CleanServerList()
        {
            _logger.LogDebug("Cleaning the server list");
            if (_timerServerList != null) await _timerServerList.DisposeAsync();

            List<ServerEntry>? newServerList = new();
            foreach (ServerEntry server in _serverList)
            {
                if (server.isLocalHost == false)
                {
                    if (server.lastUpdate.AddMinutes(3) < DateTime.Now)
                    {
                        _logger.LogInformation("- Server removed as expired: {0}", server.url);
                    }
                    else
                    {
                        newServerList.Add(server);
                    }
                }
                else
                {
                    server.lastUpdate = DateTime.Now;
                    newServerList.Add(server);
                }
            }
            _serverList = new(newServerList.OrderBy(x => x.Details.Country).ThenBy(y => y.Details.City).ToList());
            _logger.LogDebug("Server list cleaned");

            // I set the next execution cycle
            _timerServerList = new Timer(async _ =>
            {
                await CleanServerList();
            }, null, 60000, Timeout.Infinite);
        }

        /// <summary>
        /// Adds a new server to the server list if it does not already exist.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public bool AddServer(ServerEntry server)
        {
            ServerEntry? foundServer = _serverList.Where(x => x.url == server.url && !x.isLocalHost).FirstOrDefault();
            
            if (foundServer == null)
            {
                server.isLocalHost = false;
                server.lastUpdate = DateTime.Now;
                _serverList.Add(server);
                _serverList = new(_serverList.OrderBy(x => x.Details.Country).ThenBy(y => y.Details.City).ToList());
                return true;
            }
            else
            {
                foundServer.lastUpdate = DateTime.Now;
                return false;
            }
        }

        /// <summary>
        /// Returns the current server information.
        /// </summary>
        /// <returns>The current server information</returns>
        public ServerEntry? GetCurrentServerInfo()
        {
            return localServer;
        }

        /// <summary>
        /// Retrives the remote server information based on the provided server entry.
        /// Used to double check the server information after an add request.
        /// </summary>
        /// <param name="serverEntry">The server to check</param>
        /// <returns>The retrived information</returns>
        public async Task<ServerEntry?> GetRemoteServerInfo(ServerEntry serverEntry)
        {
            return await _traceRouteApiClient.GetServerInfo(serverEntry);
        }
    }
}
