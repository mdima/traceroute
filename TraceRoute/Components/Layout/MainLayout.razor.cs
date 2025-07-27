using Blazored.Toast;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Schema;
using TraceRoute.Components.Molecules;
using TraceRoute.Components.Pages;
using TraceRoute.Controllers;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;
using static TraceRoute.Models.TraceResultViewModel;

[assembly: InternalsVisibleTo("UnitTests")]
namespace TraceRoute.Components.Layout
{
    public partial class MainLayout(ServerListService serverListService, BogonIPService bogonIPService, IHttpContextAccessor contextAccessor, IJSRuntime jSRuntime, 
        TracerouteService tracerouteService, TraceRouteApiClient traceRouteApiClient)
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
        private readonly TraceRouteApiClient _traceRouteApiClient = traceRouteApiClient;
        private DotNetObjectReference<MainLayout> _componentReference => DotNetObjectReference.Create(this);

        internal List<ServerEntry> serverList = new();
        internal String selectedServerUrl = "";
        private Boolean isTracing = false;
        internal string hostToTrace = "";

        internal TraceResultViewModel? traceResult;
        internal TraceHop? currentHop;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _serverListService.ServerListChanged += RefreshServerList;
            serverList = _serverListService.GetServerList();
            selectedServerUrl = serverList.Where(x => x.isLocalHost).First().url;

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
                await _jSRuntime.InvokeVoidAsync("setDotNetHelper", _componentReference);
            }
        }

        internal String? ShowServerEntry(ServerEntry serverEntry)
        {
            if (serverEntry.Details.Country != null && serverEntry.Details.City != null)
            {
                return serverEntry.Details.Country + " - " + serverEntry.Details.City + " - " + serverEntry.url;
            }
            else
            {
                return serverEntry.url;
            }
        }

        internal async void RefreshServerList()
        {
            if (!Enumerable.SequenceEqual(serverList, _serverListService.GetServerList()))
            {
                serverList = _serverListService.GetServerList();
                if (!serverList.Where(x => x.url == selectedServerUrl).Any())
                {
                    selectedServerUrl = serverList.Where(x => x.isLocalHost).First().url;
                }
                await InvokeAsync(() => {
                    StateHasChanged();
                });
            }
        }

        internal async Task BeginTraceRoute()
        {            
            isTracing = true;
            await _jSRuntime.InvokeVoidAsync("clearMarkersAndPaths");

            ServerEntry? selectedServer = serverList.FirstOrDefault(x => x.url == selectedServerUrl);
            if (selectedServer == null)
            {
                _toastService.ShowError("Please select a server to trace.");
                isTracing = false;
                return;
            }

            if (selectedServer.isLocalHost)
            {
                traceResult = await _tracerouteService.TraceRouteFull(hostToTrace);
            }
            else
            {
                traceResult = await _traceRouteApiClient.RemoteTrace(hostToTrace, selectedServer.url!);
            }

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
                        IpDetails? details = await _ipApiClient.GetTraceHopDetails(item.HopAddress);
                        if (details != null)
                        {
                            item.Details = details;
                            await _jSRuntime.InvokeVoidAsync("addMarker", new Object[] { details.Latitude!, details.Longitude!, item.Index.ToString(), item.HopAddress });
                            StateHasChanged();
                        }
                    }
                    await _jSRuntime.InvokeVoidAsync("drawPath", new[] { traceResult.Hops });
                }
            }
            isTracing = false;
        }

        internal async Task OnShowHopDetails(TraceHop hop)
        {
            currentHop = hop;
            await _jSRuntime.InvokeVoidAsync("showModalDetails");
        }

        [JSInvokable("OnShowIpDetails")]
        public async Task OnShowIpDetails(String iPAddress)
        {
            if (traceResult == null) return;

            TraceHop? details = traceResult.Hops.Where(x => x.HopAddress == iPAddress).FirstOrDefault();

            if (details == null)
            {
                _toastService.ShowError("Unable to retrieve details for IP: " + iPAddress);
            }
            else
            {
                currentHop = details;
                StateHasChanged();
                await _jSRuntime.InvokeVoidAsync("showModalDetails");
            }            
        }

        internal async Task ShowServerDetails()
        {
            ServerEntry? selectedServer = serverList.FirstOrDefault(x => x.url == selectedServerUrl);
            if (selectedServer != null)
            {
                TraceHop traceHop = new()
                {
                    Details = selectedServer.Details,
                    HopAddress = selectedServer.url!,
                };
                await OnShowHopDetails(traceHop);
            }
        }
    }
}
