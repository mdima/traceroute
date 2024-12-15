using Newtonsoft.Json;

namespace TraceRoute.Models
{
    /// <summary>
    /// Extends the IpApiResponse with additional properties needed for the server list.
    /// </summary>
    public class ServerEntry : IpApiResponse
    {
        public ServerEntry() { }

        public ServerEntry(IpApiResponse sourceObject)
        {
            this.query = sourceObject.query;
            this.status = sourceObject.status;
            this.continent = sourceObject.continent;
            this.continentCode = sourceObject.continentCode;
            this.country = sourceObject.country;
            this.countryCode = sourceObject.countryCode;
            this.region = sourceObject.region;
            this.regionName = sourceObject.regionName;
            this.city = sourceObject.city;
            this.district = sourceObject.district;
            this.zip = sourceObject.zip;
            this.lat = sourceObject.lat;
            this.lon = sourceObject.lon;
            this.timezone = sourceObject.timezone;
            this.offset = sourceObject.offset;
            this.currency = sourceObject.currency;
            this.isp = sourceObject.isp;
            this.org = sourceObject.org;
            this._as = sourceObject._as;
            this.asname = sourceObject.asname;
            this.mobile = sourceObject.mobile;
            this.proxy = sourceObject.proxy;
            this.hosting = sourceObject.hosting;
        }

        public DateTime lastUpdate { get; set; }
        public bool isOnline { get; set; }
        public string? url { get; set; }
        public bool isLocalHost { get; set; }

        public bool Equals(ServerEntry sampleToCompare)
        {
            string myself = JsonConvert.SerializeObject(this);
            string other = JsonConvert.SerializeObject(sampleToCompare);

            return myself == other;
        }
    }
}
