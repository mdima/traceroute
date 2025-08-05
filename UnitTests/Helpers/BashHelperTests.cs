using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Services;

namespace UnitTests.Helpers
{

    public class BashHelperTests
    {
        [Fact]
        public async Task TraceKnown()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            string trace = "traceroute -n -m 30 -w1 -I -q 1 192.188.248.215";
            var traceResult = await trace.Bash();
            var hops = traceResult.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            if (hops[0].Contains("tracert"))
            {
                hops.RemoveAt(0);
            }
            Assert.True(hops.Count > 0);
        }
    }
}
