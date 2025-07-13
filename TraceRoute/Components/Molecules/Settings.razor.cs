using System.Net.NetworkInformation;
using TraceRoute.Helpers;
using TraceRoute.Models;
using TraceRoute.Services;

namespace TraceRoute.Components.Molecules
{

    public partial class Settings(IHttpContextAccessor _context, IpApiClient _ipApiClient)
    {

        SettingsViewModel settings = new();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            settings = ConfigurationHelper.GetCurrentSettings(_context.HttpContext!.Request);
            IpDetails? currentServerInfo = await _ipApiClient.GetCurrentServerDetails();
            if (currentServerInfo != null)
            {
                settings.ServerLocation = string.Format("{0} - {1}", currentServerInfo.Country, currentServerInfo.City);
            }
        }
    }
}
