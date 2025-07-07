using Microsoft.JSInterop;
using System.Net;
using System.Runtime;
using System.Threading.Tasks;
using TraceRoute.Components.Pages;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

namespace TraceRoute.Components.Layout
{
    public partial class MainLayout(ServerListService serverListService, BogonIPService bogonIPService, IHttpContextAccessor contextAccessor, IJSRuntime jSRuntime, ILoggerFactory LoggerFactory, IpApiClient IpApiClient, ReverseLookupService reverseLookupService)
    {
        private readonly ILogger _logger = LoggerFactory.CreateLogger<APIController>();
        private readonly ServerListService _serverListService = serverListService;
        private readonly BogonIPService _bogonIPService = bogonIPService;
        private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
        private readonly IJSRuntime _jSRuntime = jSRuntime;
        private readonly IpApiClient _ipApiClient = IpApiClient;
        private readonly ReverseLookupService _reverseLookupService = reverseLookupService;

        private List<ServerEntry> serverList = new();
        private String selectedServerUrl = "";
        private Boolean isTracing = false;
        private string hostToTrace = "";

        private TraceHop? currentHop = null;

        Home? homePage;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            serverList = _serverListService.GetServerList();

            if (_contextAccessor.HttpContext != null && _contextAccessor.HttpContext.Connection.RemoteIpAddress != null)
            {
                string clientIPAddress = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                if (!_bogonIPService.IsBogonIP(clientIPAddress))
                {
                    hostToTrace = clientIPAddress;
                }
            }            
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {            
            if (firstRender)
            {
                homePage.OnShowIpDetails += ShowIpDetails;
                await _jSRuntime.InvokeVoidAsync("initMap");
            }
        }

        private String? ShowServerEntry(ServerEntry serverEntry)
        {
            if (serverEntry.country != null && serverEntry.city != null)
            {
                return serverEntry.country + " - " + serverEntry.city + " - " + serverEntry.url;
            }
            else
            {
                return serverEntry.url;
            }
        }

        private void RefreshServerList()
        {
            serverList = _serverListService.GetServerList();
        }

        public async Task BeginTraceRoute()
        {            
            isTracing = true;

            TraceResultViewModel traceResult = new();

            try
            {
                _logger.LogInformation("Requested Trace to: {0}", hostToTrace);
                List<string> hops = await TraceHelper.TraceRoute(hostToTrace);

                if (hops.Count == 0)
                {
                    traceResult.ErrorDescription = "Bad request";                    
                }
                else
                {
                    foreach (string hop in hops)
                    {
                        var hopData = hop.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (!hopData[1].Contains('*'))
                        {
                            TraceHop t = new()
                            {
                                HopAddress = hopData[1],
                                TripTime = float.Parse(hopData[2]),
                                Details = new()
                                {
                                    IsBogonIP = _bogonIPService.IsBogonIP(hopData[1])
                                }
                            };
                            traceResult.Hops.Add(t);
                        }
                    }
#if DEBUG
                    //In case of a debug I some fake hop
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "15.161.156.80", TripTime = Random.Shared.NextSingle() * 10 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "213.205.32.10", TripTime = Random.Shared.NextSingle() * 10 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "62.101.124.129", TripTime = Random.Shared.NextSingle() * 10 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "93.63.100.249", TripTime = Random.Shared.NextSingle() * 10 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "4.14.49.2", TripTime = Random.Shared.NextSingle() * 10 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "66.219.34.194", TripTime = Random.Shared.NextSingle() * 10 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "208.123.73.4", TripTime = Random.Shared.NextSingle() * 10 });
                    traceResult.Hops.Add(new TraceHop() { HopAddress = "208.123.73.68", TripTime = Random.Shared.NextSingle() * 10 });
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracing to the IP Address: {0}", hostToTrace);
                traceResult.ErrorDescription = "Error while tracing";
            }

            if (traceResult.ErrorDescription != String.Empty)
            {
                await _jSRuntime.InvokeVoidAsync("showError", traceResult.ErrorDescription);
            }
            else
            {
                foreach (var item in traceResult.Hops)
                {
                    item.Index = traceResult.Hops.IndexOf(item) + 1;
                }
                homePage.setHops(traceResult.Hops);
            }            

            isTracing = false;

            // I start to get the information about the found IP addresses
            if (traceResult.ErrorDescription == String.Empty)
            {
                foreach (TraceHop item in traceResult.Hops)
                {
                    if (item.Details.IsBogonIP)
                    {
                        item.Details.City = "-";
                        item.Details.ISP = "Internal IP address";
                    }
                    else
                    {
                        IpApiResponse? response = await _ipApiClient.Get(item.HopAddress, new CancellationToken());
                        if (response != null && response.status != "fail")
                        {
                            item.Details.City = response.city;
                            item.Details.Country = response.country;
                            item.Details.ErrorDescription = "";
                            item.Details.ISP = response.isp;
                            item.Details.Latitude = response.lat;
                            item.Details.Longitude = response.lon;
                            item.Details.HostName = await _reverseLookupService.GetHostName(item.HopAddress);
                            item.Details.IsBogonIP = _bogonIPService.IsBogonIP(item.HopAddress);
                            item.Details.IsHosting = response.hosting ?? false;
                            item.Details.IsMobile = response.mobile ?? false;
                            item.Details.IsProxy = response.proxy ?? false;
                            item.Details.As = response._as;
                            item.Details.AsName = response.asname;
                            item.Details.Url = response.query;
                            item.Details.Query = response.query;
                        }
                    }
                    homePage.setHops(traceResult.Hops);
                }          
            }
        }

        public void ShowIpDetails(object? sender, TraceHop hop)
        {
            Console.WriteLine("OnShowIpDetails" + hop.Details.HostName);
            currentHop = hop;
            _jSRuntime.InvokeVoidAsync("$('#modalIpDetails').modal('show');alert('here')").GetAwaiter().GetResult();
            StateHasChanged();
        }
    }
}
