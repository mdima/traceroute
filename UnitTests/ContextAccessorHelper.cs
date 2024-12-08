using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public static class ContextAccessorHelper
    {
        public static IHttpContextAccessor GetContext(string UrlRequest, string HostName)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = UrlRequest;
            context.Request.Host = new HostString(HostName);
            context.Request.Scheme = "http";

            HttpContextAccessor obj = new()
            {
                HttpContext = context
            };
            return obj;
        }
    }
}
