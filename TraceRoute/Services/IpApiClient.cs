using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using TraceRoute.Helpers;
using TraceRoute.Models;
using static TraceRoute.Models.TraceResultViewModel;

namespace TraceRoute.Services
{
    /// <summary>
    /// Service that interacts with IP-API.com
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="logger">The Logger service</param>
    /// <param name="MemoryCache">The Memory Cache service</param>
    /// <param name="reverseLookupService">The ReverseLookupService service</param>
    public class IpApiClient(HttpClient httpClient, ILogger<IpApiClient> logger, IMemoryCache MemoryCache, ReverseLookupService reverseLookupService)
    {
        internal static string BASE_URL = "http://ip-api.com";
        private readonly HttpClient _httpClient = httpClient;
        private readonly IMemoryCache _MemoryCache = MemoryCache;
        private readonly ReverseLookupService _reverseLookupService = reverseLookupService;
        private readonly ILogger _logger = logger;
        internal DateTime _quotaReset = DateTime.MinValue;
        internal String? requestLimit;
        internal String? requestTtl;

        /// <summary>
        /// Retrives the IP information from IP-API.com
        /// </summary>
        /// <param name="ipAddress">The IP Address to query</param>
        /// <param name="ct">The cancellation token for an async operation</param>
        /// <returns>The IP address information</returns>
        public async Task<IpApiResponse?> Get(string? ipAddress, CancellationToken ct = default)
        {
            string cacheName = "IP_Info_" + ipAddress;

            try
            {
                if (IPAddress.TryParse(ipAddress, out IPAddress? checkIP))
                {
                    IpApiResponse? response = _MemoryCache.Get<IpApiResponse>(cacheName);
                    if (response == null)
                    {
                        if (_quotaReset < DateTime.Now)
                        {
                            _logger.LogDebug("Asking the IP information for IP: {0}", ipAddress);
                            if (ipAddress == "127.0.0.1") ipAddress = "";
                            string route = $"{BASE_URL}/json/{ipAddress}?fields=status,message,continent,continentCode,country,countryCode,region,regionName,city,district,zip,lat,lon,timezone,offset,currency,isp,org,as,asname,reverse,mobile,proxy,hosting,query";
                            HttpResponseMessage httpResponse = await _httpClient.GetAsync(route, ct);
                            response = await httpResponse.Content.ReadFromJsonAsync<IpApiResponse>(ct);
                            _logger.LogDebug("Result: {0}", JsonConvert.SerializeObject(response));
                            if (response != null)
                            {
                                _MemoryCache.Set(cacheName, response, DateTimeOffset.Now.AddMinutes(ConfigurationHelper.GetCacheMinutes()));
                            }
                            // I take care of the usage quota
                            requestLimit = httpResponse.Headers.GetValues("X-Rl").FirstOrDefault();
                            if (requestLimit != null && requestLimit == "0")
                            {
                                requestTtl = httpResponse.Headers.GetValues("X-Ttl").First();   // I assume this is valued
                                _quotaReset = DateTime.Now.AddSeconds(int.Parse(requestTtl));
                                _logger.LogWarning("IP API quota reaced. Sleeping for {0} seconds", requestTtl);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Cannot make more requests to IP API until: {0}", _quotaReset);
                        }
                    }
                    return response;
                }
                else
                {
                    _logger.LogWarning("Incorrect IP address received for parsing: {0}", ipAddress);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting the IP information for IP: {0}, Err: {1}", ipAddress, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrives the current server information
        /// </summary>
        /// <returns>The current server details</returns>
        public async Task<IpDetails?> GetCurrentServerDetails()
        {
            return await GetTraceHopDetails("127.0.0.1");
        }

        /// <summary>
        /// Returns the trace hop details for the given IP address as TraceHopDetails object.
        /// </summary>
        /// <param name="ipAddress">The IP address to query</param>
        /// <returns>TraceHopDetails object containing the information about the IP address</returns>
        public async Task<IpDetails?> GetTraceHopDetails(string? ipAddress)
        {
            IpApiResponse? response = await Get(ipAddress, new CancellationToken());

            if (response != null && response.status != "fail")
            {
                IpDetails result = new();

                result.Continent = response.continent;
                result.City = response.city;
                result.District = response.district;
                result.Country = response.country;
                result.CountryCode = response.countryCode;
                result.Zip = response.zip;
                result.Region = response.region;
                result.RegionName = response.regionName;
                result.ErrorDescription = "";
                result.ISP = response.isp;
                result.Organization = response.org;
                result.Latitude = response.lat;
                result.Longitude = response.lon;
                result.HostName = await _reverseLookupService.GetHostName(ipAddress!);
                result.IsBogonIP = false;
                result.IsHosting = response.hosting ?? false;
                result.IsMobile = response.mobile ?? false;
                result.IsProxy = response.proxy ?? false;
                result.As = response._as;
                result.AsName = response.asname;
                result.Url = response.query;
                result.Query = response.query;

                return result;
            }
            else             
            {
                return null;
            }
        }
    }
}