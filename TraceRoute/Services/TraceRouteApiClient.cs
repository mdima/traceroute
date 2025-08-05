using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using TraceRoute.Helpers;
using TraceRoute.Models;

namespace TraceRoute.Services
{
    /// <summary>
    /// Service that interacts with the root node of the TraceRoute service
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <param name="logger">The Logger service</param>
    public class TraceRouteApiClient(HttpClient httpClient, ILogger<TraceRouteApiClient> logger)
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger _logger = logger;
        internal String rootNodeBaseAddress = ConfigurationHelper.GetRootNode();

        /// <summary>
        /// Send the presence of the current server to the root node
        /// </summary>
        /// <param name="localServer">The current server information</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>True if the action succeeds</returns>
        public async Task<bool> SendPresence(ServerEntry localServer, CancellationToken cancellationToken)
        {           
            try
            {
                _logger.LogDebug("Sending the presence to: {0}", rootNodeBaseAddress);

                string url = $"{rootNodeBaseAddress}api/presence";
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync<ServerEntry>(url, localServer, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    Boolean result = await response.Content.ReadFromJsonAsync<Boolean>(cancellationToken);
                    _logger.LogDebug("Presence result: {0}", result);
                    return result;
                }
                else
                {
                    _logger.LogError("Error sending the presence to the root server: {0}, {1}", response.StatusCode, response.ReasonPhrase);
                    return false;
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sending the presence to the root server: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Retrives the server list from the root node
        /// </summary>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns>The list of the registered nodes</returns>
        public async Task<List<ServerEntry>?> GetServerList(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Asking the server list the presence to: {0}", rootNodeBaseAddress);

                string url = $"{rootNodeBaseAddress}api/serverlist";
                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    List<ServerEntry> result = (await response.Content.ReadFromJsonAsync<List<ServerEntry>>(cancellationToken))!;

                    _logger.LogDebug("Number of server received: {0}", result.Count);
                    return result;
                }
                else
                {
                    _logger.LogError("Error asking the list of the server to the root server: {0}, {1}", response.StatusCode, response.ReasonPhrase);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asking the list of the server: {0}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrives the server information from a given server
        /// </summary>
        /// <param name="serverEntry">The server to get the information from</param>
        /// <returns></returns>
        public async Task<ServerEntry?> GetServerInfo(ServerEntry serverEntry)
        {

            try
            {
                _logger.LogDebug("Asking the server info the presence to: {0}", serverEntry.url);

                string url = $"{serverEntry.url}api/serverInfo";
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    ServerEntry result = (await response.Content.ReadFromJsonAsync<ServerEntry>())!;
                    _logger.LogDebug("Successfully received the information from {0}", serverEntry.url);
                    return result;
                }
                else
                {
                    _logger.LogError("Error asking the server info from the server: {0}, {1}, {2}", serverEntry.url, response.StatusCode, response.ReasonPhrase);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asking the server info from the server: {0}, {1}", serverEntry.url, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrives the server information from a given server
        /// </summary>
        /// <param name="HostToTrace">The IP to trace</param>
        /// <param name="RemoteServerUrl">The remote TraceRoute server</param>
        /// <returns>The result of the traceroute</returns>
        public async Task<TraceResultViewModel> RemoteTrace(string HostToTrace, string RemoteServerUrl)
        {
            TraceResultViewModel result = new();
            string traceError = "";

            try
            {
                _logger.LogDebug("Asking to trace the IP {0} to the server {1}", HostToTrace, RemoteServerUrl);
                
                string url = $"{RemoteServerUrl}api/trace/{HostToTrace}";
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    result = (await response.Content.ReadFromJsonAsync<TraceResultViewModel>())!;
                    _logger.LogDebug("Successfully received the traceroute from {0}", RemoteServerUrl);
                }
                else
                {
                    traceError = String.Format("Error asking the trace result from the server: {0}, {1}, {2}", RemoteServerUrl, response.StatusCode, response.ReasonPhrase);
                    _logger.LogError("{traceError}", traceError);
                }
            }
            catch (Exception ex)
            {
                traceError = String.Format("Error asking the trace result from the server: {0}, {1}", RemoteServerUrl, ex.Message);
                _logger.LogError(ex, "{traceError}", traceError);
            }

            if (traceError != "")
            {
                result = new TraceResultViewModel()
                {
                    ErrorDescription = traceError
                };
            }

            return result!;
        }
    }
}
