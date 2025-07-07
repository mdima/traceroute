using log4net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TraceRoute.Services
{
    public class StoreServerURLFilter : IMiddleware
    {
        protected static string ServerURL = "";
        ILog _logger = LogManager.GetLogger("StoreServerURLFilter");

        public string GetServerURL()
        {
            return ServerURL;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (string.IsNullOrEmpty(ServerURL))
            {
                string uriString = $"{context.Request.Scheme}://{context.Request.Host}/";
                if (Uri.TryCreate(uriString, UriKind.Absolute, out var location))
                {
                    ServerURL = location.AbsoluteUri;
                    _logger.Info("Local server URI set to: " + ServerURL);
                }
                else
                {
                    _logger.Warn("Cannot process the local server URI: " + uriString);
                }
            }
            await next(context);
        }
    }
}
