using System.Net.NetworkInformation;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;

namespace TraceRoute.Components.Molecules
{

    public partial class Settings(IHttpContextAccessor context, IpApiClient ipApiClient)
    {

        internal SettingsViewModel settings = new();

        private readonly IHttpContextAccessor _context = context;
        private readonly IpApiClient _ipApiClient = ipApiClient;

        protected override async Task OnInitializedAsync()
        {
            if (_context.HttpContext != null)
            {
                settings = ConfigurationHelper.GetCurrentSettings(_context.HttpContext.Request);
            }
            IpDetails? currentServerInfo = await _ipApiClient.GetCurrentServerDetails();
            if (currentServerInfo != null)
            {
                settings.ServerLocation = string.Format("{0} - {1}", currentServerInfo.Country, currentServerInfo.City);
            }
        }
    }
}
