﻿using System;
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
        public async Task<TraceResultViewModel> TraceRoute(string destination)
        {
            TraceResultViewModel response = new();

            try
            {
                _logger.LogInformation("Requested Trace to: {0}", destination);
                List<string> hops = await TraceHelper.TraceRoute(destination);

                if (hops.Count() == 0)
                {
                    response.ErrorDescription = "Bad request";
                    return response;
                }

                foreach (string hop in hops)
                {
                    var hopData = hop.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (!hopData[1].Contains("*"))
                    {
                        TraceHop t = new()
                        {
                            HopAddress = hopData[1],
                            TripTime = float.Parse(hopData[2]),
                            Details = new() { 
                                IsBogonIP = _bogonIPService.IsBogonIP(hopData[1]) 
                            }
                        };
                        response.Hops.Add(t);
                    }
                }
#if DEBUG
                //In case of a debug I some fake hop
                response.Hops.Add(new TraceHop() { HopAddress = "15.161.156.80", TripTime = 10});
                response.Hops.Add(new TraceHop() { HopAddress = "213.205.32.10", TripTime = 11 });
                response.Hops.Add(new TraceHop() { HopAddress = "62.101.124.129", TripTime = 12 });
                response.Hops.Add(new TraceHop() { HopAddress = "93.63.100.249", TripTime = 13 });
                response.Hops.Add(new TraceHop() { HopAddress = "4.14.49.2", TripTime = 14 });
                response.Hops.Add(new TraceHop() { HopAddress = "66.219.34.194", TripTime = 15 });
                response.Hops.Add(new TraceHop() { HopAddress = "208.123.73.4", TripTime = 16 });
                response.Hops.Add(new TraceHop() { HopAddress = "208.123.73.68", TripTime = 17 });
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracing to the IP Address: {0}", destination);
                response.ErrorDescription = "Error while tracing";                
            }
            return response;
        }

        /// <summary>
        /// Retrives the information about the given IP address
        /// </summary>
        /// <param name="ipAddress">The IP address to request</param>
        /// <returns>The IP information</returns>
        [HttpGet("api/ipinfo/{ipAddress}")]
        public async Task<TraceHopDetails> IPInfo(string ipAddress)
        {
            TraceHopDetails result = new();

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
        /// Retrives the current configuration
        /// </summary>
        [HttpGet("api/settings")]
        public async Task<SettingsViewModel> GetSettings()
        {
            SettingsViewModel result = ConfigurationHelper.GetCurrentSettings(Request);
            IpApiResponse? currentServerInfo = await _ipApiClient.GetCurrentServerDetails();
            if (currentServerInfo != null)
            {
                result.ServerLocation = string.Format("{0} - {1}", currentServerInfo.city, currentServerInfo.country);
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
