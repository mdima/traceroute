using Newtonsoft.Json;

namespace TraceRoute.Models
{
    /// <summary>
    /// The information about a TraceRoute server.
    /// </summary>
    public class ServerEntry
    {
        public DateTime lastUpdate { get; set; }
        public bool isOnline { get; set; }
        public string url { get; set; } = "Localhost";
        public bool isLocalHost { get; set; }
        public IpDetails Details { get; set; } = new();

        public bool Equals(ServerEntry sampleToCompare)
        {
            string myself = JsonConvert.SerializeObject(this);
            string other = JsonConvert.SerializeObject(sampleToCompare);

            return myself == other;
        }
    }
}
