using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static TraceRoute.Models.TraceResultViewModel;

namespace TraceRoute.Components.Pages
{
    public partial class Home 
    {
        private List<TraceHop> Hops = new();
        public event EventHandler<TraceHop>? OnShowIpDetails;

        public void setHops(List<TraceHop> hops)
        {
            Hops = hops;
            StateHasChanged();
        }

        public void IpDetails(TraceHop hop)
        {
            OnShowIpDetails?.Invoke(this, hop);
        }
    }
}
