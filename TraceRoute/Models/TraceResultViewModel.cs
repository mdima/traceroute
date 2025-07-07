using Newtonsoft.Json;
using System.Configuration;

namespace TraceRoute.Models
{
    public class TraceResultViewModel
    {
        public string ErrorDescription { get; set; } = string.Empty;

        public List<TraceHop> Hops { get; set; } = new();

        public class TraceHop
        {
            public int Index { get; set; } = 0;
            public string HopAddress { get; set; } = string.Empty;
            public float TripTime { get; set; } = 0;
            public TraceHopDetails Details { get; set; } = new();
        }

        public class TraceHopDetails
        {
            public bool IsBogonIP { get; set; } 
            public string ErrorDescription { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? Continent { get; set; }
            public string? Country { get; set; }
            public string? City { get; set; }
            public string? ISP { get; set; }
            public string? HostName { get; set; }
            public bool? IsMobile { get; set; }
            public bool? IsProxy { get; set; }
            public bool? IsHosting { get; set; }
            public string? As { get; set; }
            public string? AsName { get; set; }
            public string? Query { get; set; }
            public string? Url { get; set; }
        }
    }
}
