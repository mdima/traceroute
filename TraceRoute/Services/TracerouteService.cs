using Microsoft.AspNetCore.Components;
using System.Reflection.Metadata.Ecma335;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using static TraceRoute.Models.TraceResultViewModel;

namespace TraceRoute.Services
{
    public class TracerouteService
    {
        private readonly BogonIPService _bogonIPService;

        private readonly ILogger<TracerouteService> _logger;

        public TracerouteService(BogonIPService bogonIPService, ILogger<TracerouteService> logger) {
            _logger = logger;
            _bogonIPService = bogonIPService;
        }

        public async Task<TraceResultViewModel> Trace(string hostToTrace)
        {
            TraceResultViewModel traceResult = new();

            try
            {
                _logger.LogInformation("Requested Trace to: {0}", hostToTrace);
                List<string> hops = await TraceHelper.TraceRoute(hostToTrace);

                if (hops.Count == 0)
                {
                    traceResult.ErrorDescription = "Bad request";
                }
                else
                {
                    foreach (string hop in hops)
                    {
                        var hopData = hop.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (!hopData[1].Contains('*'))
                        {
                            TraceHop t = new()
                            {
                                HopAddress = hopData[1],
                                TripTime = float.Parse(hopData[2]),
                                Details = new()
                                {
                                    IsBogonIP = _bogonIPService.IsBogonIP(hopData[1])
                                }
                            };
                            traceResult.Hops.Add(t);
                        }
                    }
#if DEBUG
                    //In case of a debug I some fake hop
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "15.161.156.80", TripTime = Random.Shared.NextSingle() * 10, Index = 3 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "213.205.32.10", TripTime = Random.Shared.NextSingle() * 10, Index = 4 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "62.101.124.129", TripTime = Random.Shared.NextSingle() * 10, Index = 5 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "93.63.100.249", TripTime = Random.Shared.NextSingle() * 10, Index = 6 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "4.14.49.2", TripTime = Random.Shared.NextSingle() * 10, Index = 7 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "66.219.34.194", TripTime = Random.Shared.NextSingle() * 10, Index = 8 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "208.123.73.4", TripTime = Random.Shared.NextSingle() * 10, Index = 9 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "208.123.73.68", TripTime = Random.Shared.NextSingle() * 10, Index = 10 });
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracing to the IP Address: {0}", hostToTrace);
                traceResult.ErrorDescription = "Error while tracing";
            }

            return traceResult;
        }        
    }
}
