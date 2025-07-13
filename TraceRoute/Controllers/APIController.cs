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
    public class APIController(ILoggerFactory LoggerFactory, IpApiClient IpApiClient, BogonIPService bogonIPService, ReverseLookupService reverseLookupService, ServerListService serverListService) : Controller
    {
        private readonly ILogger _logger = LoggerFactory.CreateLogger<APIController>();
        private readonly IpApiClient _ipApiClient = IpApiClient;
        private readonly BogonIPService _bogonIPService = bogonIPService;
        private readonly ReverseLookupService _reverseLookupService = reverseLookupService;
        private readonly ServerListService _serverListService = serverListService;

        /// <summary>
        /// Performs traceroute on specified hostname.
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
                result = await TraceHelper.TraceRoute(destination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracing to the IP Address from remote: {0}", destination);                
            }
            return result;
        }

        /// <summary>
        /// Retrives the information about the given IP address
        /// </summary>
        /// <param name="ipAddress">The IP address to request</param>
        /// <returns>The IP information</returns>
        [HttpGet("api/ipinfo/{ipAddress}")]
        public async Task<IpDetails> IPInfo(string ipAddress)
        {
            IpDetails result = new();

            try
            {
                _logger.LogInformation("Requested IPInfo for: {0}", ipAddress);

                if (!_bogonIPService.IsBogonIP(ipAddress))
                {
                    IpApiResponse? ipInfo = await _ipApiClient.Get(ipAddress, new CancellationToken());
                    if (ipInfo != null && ipInfo.status != "fail")
                    {
                        result.City = ipInfo.city;
                        result.Country = ipInfo.country;
                        result.CountryCode = ipInfo.countryCode;
                        result.Region = ipInfo.region;
                        result.RegionName = ipInfo.regionName;
                        result.ErrorDescription = "";
                        result.ISP = ipInfo.isp;
                        result.Latitude = ipInfo.lat;
                        result.Longitude = ipInfo.lon;
                        result.HostName = await _reverseLookupService.GetHostName(ipAddress);
                    }
                    else
                    {
                        result.ErrorDescription = "Could not retrive the IP information";
                    }
                }
                else
                {
                    result.IsBogonIP = true;
                    result.ISP = "Internal IP address";
                    result.ErrorDescription = "";
                    result.HostName = await _reverseLookupService.GetHostName(ipAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting the IP information: {0}", ipAddress);
                result.ErrorDescription = "Error";
            }
            return result;
        }

        /// <summary>
        /// Retrives the detailed information about the given IP address
        /// </summary>
        /// <param name="ipAddress">The IP address to request</param>
        /// <returns>The IP information</returns>
        [HttpGet("api/ipdetails/{ipAddress}")]
        public async Task<IpApiResponse> IPDetails(string ipAddress)
        {
            IpApiResponse result = new();

            try
            {
                _logger.LogInformation("Requested IPDetails for: {0}", ipAddress);

                if (!_bogonIPService.IsBogonIP(ipAddress))
                {
                    IpApiResponse? response = await _ipApiClient.Get(ipAddress, new CancellationToken());
                    if (response != null)
                    {
                        result = response;
                    }
                    else
                    {
                        result.status = "Could not retrive the IP information";
                    }
                }
                else
                {
                    result.status = "BogonIP";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting the IP details: {0}", ipAddress);
                result.status = "Error";
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
        /// Returns the server list
        /// </summary>
        [HttpGet("api/serverlist")]
        public List<ServerEntry> GetServerList()
        {
            return _serverListService.GetServerList();
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
