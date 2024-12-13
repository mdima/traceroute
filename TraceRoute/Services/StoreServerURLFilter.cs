using log4net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TraceRoute.Services
{
    public class StoreServerURLFilter : IAsyncActionFilter
    {
        protected static string ServerURL = "";
        ILog _logger = LogManager.GetLogger("StoreServerURLFilter");

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (string.IsNullOrEmpty(ServerURL))
            {
                string uriString = $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}/";
                if (Uri.TryCreate(uriString, UriKind.Absolute, out var location))
                {
                    ServerURL = location.AbsoluteUri;
                }
                else
                {
                    _logger.Warn("Cannot process the local server URI: " + uriString);
                }
            }
            await next();
        }

        public string getServerURL()
        {
            return ServerURL;
        }
    }
}
