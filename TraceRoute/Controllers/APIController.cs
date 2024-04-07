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
    public class APIController(ILoggerFactory LoggerFactory, IpApiClient IpApiClient, BogonIPService bogonIPService) : Controller
    {
        private readonly ILogger _logger = LoggerFactory.CreateLogger<APIController>();
        private readonly IpApiClient _ipApiClient = IpApiClient;
        private readonly BogonIPService _bogonIPService = bogonIPService;

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
                string trace = "traceroute -n -m 30 -w1 -I -q 1 " + destination;
                var traceResult = await trace.Bash();
                var hops = traceResult.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                if (hops[0].Contains("traceroute"))
                {
                    hops.RemoveAt(0);
                }

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
                            TripTime = float.Parse(hopData[2])
                        };
                        response.Hops.Add(t);
                    }
                }
                //response.Hops.Add(new TraceHop() { HopAddress= "15.161.156.80", TripTime = 80 });
                //response.Hops.Add(new TraceHop() { HopAddress = "213.205.32.10", TripTime = 90 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracing to the IP Address: {0}", destination);
                response.ErrorDescription = "Error while tracing";                
            }
            return response;
        }

        [HttpGet("api/ipinfo/{ipAddress}")]
        public async Task<TraceHopDetails> IPInfo(string ipAddress)
        {
            TraceHopDetails result = new();

            try
            {
                if (!_bogonIPService.IsBogonIP(ipAddress))
                {
                    IpApiResponse? ipInfo = await _ipApiClient.Get(ipAddress, new CancellationToken());
                    if (ipInfo != null)
                    {
                        result.City = ipInfo.city;
                        result.Country = ipInfo.country;
                        result.ErrorDescription = "";
                        result.ISP = ipInfo.isp;
                        result.Latitude = ipInfo.lat;
                        result.Longitude = ipInfo.lon;
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting the IP information: {0}", ipAddress);
                result.ErrorDescription = "Error";
            }
            return result;
        }
    }
}
