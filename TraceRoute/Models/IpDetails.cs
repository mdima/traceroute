namespace TraceRoute.Models
{
    public class IpDetails
    {
        public bool IsBogonIP { get; set; }
        public string ErrorDescription { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Continent { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public string? Region { get; set; }
        public string? RegionName { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Zip { get; set; }
        public string? Timezone { get; set; }
        public string? Currency { get; set; }
        public string? ISP { get; set; }
        public string? Organization { get; set; }
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
