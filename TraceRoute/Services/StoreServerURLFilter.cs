using Microsoft.AspNetCore.Mvc.Filters;

namespace TraceRoute.Services
{
    public class StoreServerURLFilter : IAsyncActionFilter
    {
        protected static string ServerURL = "";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (string.IsNullOrEmpty(ServerURL))
            {
                var location = new Uri($"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}/");

                ServerURL = location.AbsoluteUri;
            }
            await next();
        }

        public string getServerURL()
        {
            return ServerURL;
        }
    }
}
