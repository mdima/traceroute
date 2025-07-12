using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static TraceRoute.Models.TraceResultViewModel;

namespace TraceRoute.Components.Pages
{
    public partial class Home 
    {
        [Parameter]
        public EventCallback<TraceHop> OnShowIpDetails { get; set; }

        [Parameter]
        public List<TraceHop> Hops { get; set; } = new();

        public async Task IpDetails(TraceHop hop)
        {
            if (OnShowIpDetails.HasDelegate)
            {
                await OnShowIpDetails.InvokeAsync(hop);
            }
        }
    }
}
