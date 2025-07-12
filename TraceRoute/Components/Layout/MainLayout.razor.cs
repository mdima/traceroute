using Blazored.Toast;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
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
    public partial class MainLayout(ServerListService serverListService, BogonIPService bogonIPService, IHttpContextAccessor contextAccessor, IJSRuntime jSRuntime, TracerouteService tracerouteService)
    {

        [Inject]
        private IpApiClient _ipApiClient { get; set; } = default!;

        [Inject]
        private ReverseLookupService _reverseLookupService { get; set; } = default!;

        [Inject]
        private IToastService _toastService { get; set; } = default!;

        private readonly ServerListService _serverListService = serverListService;
        private readonly BogonIPService _bogonIPService = bogonIPService;
        private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
        private readonly IJSRuntime _jSRuntime = jSRuntime;
        private readonly TracerouteService _tracerouteService = tracerouteService;

        private List<ServerEntry> serverList = new();
        private String selectedServerUrl = "";
        private Boolean isTracing = false;
        private string hostToTrace = "";

        private TraceResultViewModel? traceResult;
        private TraceHop? currentHop;

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
            await _jSRuntime.InvokeVoidAsync("clearMarkersAndPaths");

            traceResult = await _tracerouteService.Trace(hostToTrace);            

            if (traceResult.ErrorDescription != String.Empty)
            {
                _toastService.ShowError(traceResult.ErrorDescription);
            }
            else
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
                            item.Details.Continent = response.continent;
                            item.Details.City = response.city;
                            item.Details.District = response.district;
                            item.Details.Country = response.country;
                            item.Details.CountryCode = response.countryCode;
                            item.Details.Region = response.region;
                            item.Details.RegionName = response.regionName;
                            item.Details.ErrorDescription = "";
                            item.Details.ISP = response.isp;
                            item.Details.Organization = response.org;
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
                            await _jSRuntime.InvokeVoidAsync("addMarker", new Object[] { response.lat!, response.lon!, item.Index.ToString(), item.HopAddress } );
                            StateHasChanged();
                        }
                    }
                    await _jSRuntime.InvokeVoidAsync("drawPath", new[] { traceResult.Hops });
                }
            }
            isTracing = false;
        }

        private async Task ShowIpDetails(TraceHop hop)
        {
            currentHop = hop;
            await _jSRuntime.InvokeVoidAsync("showModalDetails");
        }
    }
}
