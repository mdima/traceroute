using log4net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TraceRoute.Services
{
    /// <summary>
    /// Keep track of the local server URL.
    /// </summary>
    public class StoreServerURLFilter : IMiddleware
    {
        internal static string ServerURL = "";
        ILog _logger = LogManager.GetLogger("StoreServerURLFilter");

        /// <summary>
        /// Returns the current server URL.
        /// </summary>
        /// <returns>The current server URL</returns>
        public string GetServerURL()
        {
            return ServerURL;
        }

        /// <summary>
        /// If not saved before, retrives the local server URL from the context and saves it.
        /// </summary>
        /// <param name="context">The current HttpContext</param>
        /// <param name="next">The RequestDelegate</param>
        /// <returns></returns>
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
