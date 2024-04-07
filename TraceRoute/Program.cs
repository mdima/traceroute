using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TraceRoute.Helpers;
using TraceRoute.Services;

[assembly: InternalsVisibleTo("UnitTests")]

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddMvc();
builder.WebHost.UseIISIntegration();
builder.Services.AddHttpClient<IpApiClient>();
builder.Services.AddSingleton<BogonIPService>();
builder.Services.AddMemoryCache(x => { x.TrackStatistics = true; x.TrackLinkedCacheEntries = true; });
//Forward headers configuration for reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
//Logging
string Log4NetFile = "log4net.config";
builder.Services.AddLogging(builder => { builder.AddLog4Net(Log4NetFile); });
ILog _logger = LogManager.GetLogger("Startup");

//I build the app
var app = builder.Build();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.UseStaticFiles();

//Proxy Forwarding IP Address
var forwardingOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardingOptions.KnownNetworks.Clear(); // Loopback by default, this should be temporary
forwardingOptions.KnownProxies.Clear(); // Update to include
app.UseForwardedHeaders(forwardingOptions);

//I record the starting events
app.Lifetime.ApplicationStarted.Register(() => { _logger.Info($"Application started, environment: {builder.Environment.EnvironmentName}"); });
app.Lifetime.ApplicationStopping.Register(() => { _logger.Info("Application stopping"); });
app.Lifetime.ApplicationStopped.Register(() => { _logger.Info("Application ended"); });

//I run the app
app.Run();