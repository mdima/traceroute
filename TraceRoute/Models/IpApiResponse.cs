using System.Text.Json.Serialization;

namespace TraceRoute.Models
{
    /// <summary>
    /// This is the model from IPApi.com
    /// </summary>
    public class IpApiResponse
    {
        public string? query { get; set; }
        public string? status { get; set; }
        public string? continent { get; set; }
        public string? continentCode { get; set; }
        public string? country { get; set; }
        public string? countryCode { get; set; }
        public string? region { get; set; }
        public string? regionName { get; set; }
        public string? city { get; set; }
        public string? district { get; set; }
        public string? zip { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }
        public string? timezone { get; set; }
        public double? offset { get; set; }
        public string? currency { get; set; }
        public string? isp { get; set; }
        public string? org { get; set; }
        [JsonPropertyName("as")]
        public string? _as { get; set; }
        public string? asname { get; set; }
        public bool? mobile { get; set; }
        public bool? proxy { get; set; }
        public bool? hosting { get; set; }
    }
}
