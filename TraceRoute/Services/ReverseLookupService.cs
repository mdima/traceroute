using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TraceRoute.Helpers;
using TraceRoute.Models;

[assembly: InternalsVisibleTo("UnitTests")]
namespace TraceRoute.Services
{
    /// <summary>
    /// Service that performs the reverse DNS lookups
    /// On Windows doesn't work because of this issue: https://github.com/docker/for-win/issues/13681
    /// </summary>
    /// <param name="logger">The Logger service</param>
    /// <param name="MemoryCache">The Memory Cache service</param>
    public class ReverseLookupService(ILogger<ReverseLookupService> logger, IMemoryCache MemoryCache)
    {
        private readonly ILogger _logger = logger;
        private readonly IMemoryCache _MemoryCache = MemoryCache;

        public async Task<string> GetHostName(string ipAddress)
        {
            string cacheName = "RevLookup_" + ipAddress;

            string? result = _MemoryCache.Get<string>(cacheName);
            if (result == null)
            {
                if (IPAddress.TryParse(ipAddress, out IPAddress? address) && address != null)
                {
                    _logger.LogDebug("Asking the reverse lookup for IP: {ipAddress}", ipAddress);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        result = await GetHostNameWindows(address);
                    }
                    else
                    {
                        result = await GetHostNameLinux(address);
                    }
                }
                else
                {
                    result = "";
                }

                _MemoryCache.Set(cacheName, result, DateTimeOffset.Now.AddMinutes(ConfigurationHelper.GetCacheMinutes()));
            }
            return result;
        }

        /// <summary>
        /// Low level reverse lookup for Linux systems.
        /// </summary>
        /// <param name="address">The address to process</param>
        /// <returns>The result of the operation</returns>
        internal async Task<string> GetHostNameLinux(IPAddress address)
        {
            string result = "";

            try
            {
                string lookup = "host " + address;
                var lookupResult = await lookup.Bash();
                _logger.LogDebug("Lookup bash result: {lookupResult}", lookupResult);

                var splits = lookupResult.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                if (splits.Count > 0 && !lookupResult.Contains("not found:") && !lookupResult.Contains("has no PTR record"))
                {
                    result = splits.Last();
                    result = result.Replace(".\n", "");
                }
                else
                {
                    result = "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHostNameLinux for address: {address}, Err: {Message}", address, ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Low level reverse lookup for Windows systems.
        /// </summary>
        /// <param name="address">The address to process</param>
        /// <returns>The result of the operation</returns>
        internal async Task<string> GetHostNameWindows(IPAddress address)
        {
            try
            {
                IPHostEntry hostInfo = await Dns.GetHostEntryAsync(address);
                return hostInfo.HostName;
            }
            catch (Exception ex)
            {
                if (ex.Message == "No such host is known.")
                {
                    return "";
                }
                else
                {
                    _logger.LogError(ex, "Error in GetHostNameWindows for address: {address}, Err: {Message}", address, ex.Message);
                    return string.Empty;
                }
            }
        }
    }
}
