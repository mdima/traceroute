using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TraceRoute.Controllers;
using TraceRoute.Models;
using static TraceRoute.Models.TraceResultViewModel;

[assembly: InternalsVisibleTo("UnitTests")]
namespace TraceRoute.Services
{
    /// <summary>
    /// Provide the logic of the Traceroute operations.
    /// </summary>
    public class TracerouteService
    {
        private readonly BogonIPService _bogonIPService;

        private readonly ILogger<TracerouteService> _logger;

        public TracerouteService(BogonIPService bogonIPService, ILogger<TracerouteService> logger) {
            _logger = logger;
            _bogonIPService = bogonIPService;
        }

        /// <summary>
        /// Returns the full trace result for the given host (IP Address and full Hop information).
        /// </summary>
        /// <param name="hostToTrace">The host to trace</param>
        /// <returns>The full result of the trace operation</returns>
        public async Task<TraceResultViewModel> TraceRouteFull(string hostToTrace)
        {
            TraceResultViewModel traceResult = new();

            try
            {
                _logger.LogInformation("Requested Trace to: {0}", hostToTrace);
                List<string> hops = await TraceRoute(hostToTrace);

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
                                Index = traceResult.Hops.Count + 1,
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
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "15.161.156.80", TripTime = Random.Shared.NextSingle() * 10, Index = 3 });
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "213.205.32.10", TripTime = Random.Shared.NextSingle() * 10, Index = 4 });
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "62.101.124.129", TripTime = Random.Shared.NextSingle() * 10, Index = 5 });
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "93.63.100.249", TripTime = Random.Shared.NextSingle() * 10, Index = 6 });
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "4.14.49.2", TripTime = Random.Shared.NextSingle() * 10, Index = 7 });
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "66.219.34.194", TripTime = Random.Shared.NextSingle() * 10, Index = 8 });
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "208.123.73.4", TripTime = Random.Shared.NextSingle() * 10, Index = 9 });
                    traceResult.Hops.Insert(1, new TraceHop() { HopAddress = "208.123.73.68", TripTime = Random.Shared.NextSingle() * 10, Index = 10 });
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

        /// <summary>
        /// Returns only the array of hops for the given host.
        /// </summary>
        /// <param name="destination">The destination of the Trace operation</param>
        /// <returns>The hop list</returns>
        public async Task<List<String>> TraceRoute(String destination)
        {
            List<String> result = new();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = TraceRouteWindows(destination).ToList();
            }
            else
            {
                result = await TraceRouteLinux(destination);
            }

            // I make sure that the last entry is the destination
            String destinationIp = await EnsureIpAddress(destination);
            if (result.Count > 0 && !result.Last().Contains(destinationIp))
            {
                // I add a fake entry and the destination
                result.Add("x ... 0");
                result.Add(String.Format("x {0} 0", destinationIp));
            }
            return result;
        }

        /// <summary>
        /// Performs a traceroute on the given destination using Windows Ping.
        /// </summary>
        /// <param name="destination">The destination of the Trace operation</param>
        /// <returns>The hop list</returns>
        internal IEnumerable<string> TraceRouteWindows(string destination)
        {
            // Initial variables
            var limit = 30;
            var buffer = new byte[32];
            var pingOpts = new PingOptions(1, true);
            var ping = new Ping();

            // Result holder.
            PingReply result;

            do
            {
                result = ping.Send(destination, 1000, buffer, pingOpts);
                pingOpts = new PingOptions(pingOpts.Ttl + 1, pingOpts.DontFragment);

                if (result.Status != IPStatus.TimedOut)
                {
                    if (result.Address.ToString() != "::1")
                    {
                        yield return string.Format("{0} {1} {2} ms", pingOpts.Ttl, result.Address.ToString(), result.RoundtripTime);
                    }
                }
                else 
                {
                    yield return string.Format("x ... 0");
                    break;
                }
            }
            while (result.Status != IPStatus.Success || pingOpts.Ttl < limit);
        }

        /// <summary>
        /// Performs a traceroute on the given destination using Linux traceroute command.
        /// </summary>
        /// <param name="destination">The destination of the Trace operation</param>
        /// <returns>The hop list</returns>
        internal async Task<List<string>> TraceRouteLinux(string destination)
        {
            string trace = "traceroute -n -m 30 -w1 -I -q 1 " + destination;
            var traceResult = await trace.Bash();

            List<string> hops = traceResult.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            if (hops[0].Contains("traceroute"))
            {
                hops.RemoveAt(0);
            }

            return hops;
        }

        /// <summary>
        /// Makes sure that the given host is an IP Address, otherwise it tries to resolve it.
        /// </summary>
        /// <param name="hostToTrace"></param>
        /// <returns>The IP address of the specified host or the </returns>
        /// <exception cref="Exception"></exception>
        internal async Task<string> EnsureIpAddress(string hostToTrace)
        {
            if (IPAddress.TryParse(hostToTrace, out IPAddress? address) && address != null)
            {
                return hostToTrace;
            }
            else
            {
                try
                {
                    var addresses = await Dns.GetHostAddressesAsync(hostToTrace);
                    if (addresses.Length > 0)
                    {
                        return addresses[0].ToString();
                    }
                    else
                    {
                        return hostToTrace;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ensuring the IP of the host: {0}", hostToTrace);
                    return hostToTrace;
                }
            }
        }
    }
}
