using System.Net.NetworkInformation;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using TraceRoute.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
namespace TraceRoute.Helpers
{
    public static class TraceHelper
    {
        public static async Task<List<string>> TraceRoute(string destination)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return TraceRouteWindows(destination).ToList();
            }
            else
            {
                return await TraceRouteLinux(destination);
            }
        }

        internal static IEnumerable<string> TraceRouteWindows(string destination)
        {
            // Initial variables
            var limit = 1000;
            var buffer = new byte[32];
            var pingOpts = new PingOptions(1, true);
            var ping = new Ping();

            // Result holder.
            PingReply result;

            do
            {
                result = ping.Send(destination, 4000, buffer, pingOpts);
                pingOpts = new PingOptions(pingOpts.Ttl + 1, pingOpts.DontFragment);

                if (result.Status != IPStatus.TimedOut)
                {
                    yield return string.Format("{0} {1} {2} ms", pingOpts.Ttl, result.Address.ToString(), result.RoundtripTime);
                }
            }
            while (result.Status != IPStatus.Success && pingOpts.Ttl < limit);
        }

        internal static async Task<List<string>> TraceRouteLinux(string destination)
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
    }
}
