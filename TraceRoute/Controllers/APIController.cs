using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

namespace TraceRoute.Controllers
{
    public class APIController(ILoggerFactory LoggerFactory, ServerListService serverListService, TracerouteService tracerouteService) : Controller
    {
        private readonly ILogger _logger = LoggerFactory.CreateLogger<APIController>();
        private readonly ServerListService _serverListService = serverListService;
        private readonly TracerouteService _tracerouteService = tracerouteService;

        /// <summary>
        /// Performs traceroute on specified hostname. Used by a remote server to trace from this location.
        /// </summary>
        /// <returns>JSON array of hops and the round trip time.</returns>
        /// <param name="destination">Hostname IP / URL</param>
        [HttpGet("api/trace/{destination}")]
        public async Task<List<string>?> TraceRoute(string destination)
        {
            List<string>? result = null;

            try
            {
                _logger.LogInformation("Requested Trace from remote to: {0}", destination);
                result = await _tracerouteService.TraceRoute(destination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracing to the IP Address from remote: {0}", destination);                
            }
            return result;
        }

        /// <summary>
        /// Receive a Traceroute server information
        /// </summary>
        [HttpPost("api/presence")]
        public async Task<bool> ReceivePresence([FromBody] ServerEntry server)
        {
            _logger.LogInformation("Received presence from: {0}", server.url);

            ServerEntry? checkInfo = await _serverListService.GetRemoteServerInfo(server);
            if (checkInfo != null && checkInfo.Equals(server))
            {
                checkInfo.lastUpdate = DateTime.Now;
                checkInfo.isOnline = true;                
                _serverListService.AddServer(checkInfo);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the current server information
        /// </summary>
        [HttpGet("api/serverInfo")]
        public ServerEntry? GetServerInfo()
        {
            return _serverListService.GetCurrentServerInfo();
        }
    }
}
