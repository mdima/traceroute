using Newtonsoft.Json;
using System.Configuration;

namespace TraceRoute.Models
{
    /// <summary>
    /// The result of a Trace operation.
    /// </summary>
    public class TraceResultViewModel
    {
        public string ErrorDescription { get; set; } = string.Empty;

        public List<TraceHop> Hops { get; set; } = new();

        public class TraceHop
        {
            public int Index { get; set; } = 0;
            public string HopAddress { get; set; } = string.Empty;
            public float TripTime { get; set; } = 0;
            public IpDetails Details { get; set; } = new();
        }
    }
}
