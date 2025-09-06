using Blazored.Toast;
using log4net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using TraceRoute.Components;
using TraceRoute.Services;
using WebMarkupMin.AspNetCoreLatest;

[assembly: InternalsVisibleTo("UnitTests")]

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseIISIntegration();    //Optional for IIS deployment
builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; }); ;
builder.Services.AddRazorPages();
builder.Services.AddSingleton<StoreServerURLFilter>();
builder.Services.AddHttpClient<IpApiClient>();
builder.Services.AddHttpClient<TraceRouteApiClient>();
builder.Services.AddSingleton<BogonIPService>();
builder.Services.AddSingleton<ReverseLookupService>();
builder.Services.AddSingleton<TracerouteService>();
builder.Services.AddMemoryCache(x => { x.TrackStatistics = true; x.TrackLinkedCacheEntries = true; });
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<ServerListService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ServerListService>());
builder.Services.AddBlazoredToast();

//WebMarkupMin
builder.Services.AddWebMarkupMin(
    options =>
    {
        options.AllowMinificationInDevelopmentEnvironment = true;
        options.AllowCompressionInDevelopmentEnvironment = true;
    })
    .AddHtmlMinification(
        options =>
        {
            options.MinificationSettings.RemoveRedundantAttributes = true;
            //options.MinificationSettings.RemoveHttpProtocolFromAttributes = true;
            //options.MinificationSettings.RemoveHttpsProtocolFromAttributes = true;
        })
    .AddHttpCompression()
    .AddXhtmlMinification()
    .AddXmlMinification();
//Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes =
        ResponseCompressionDefaults.MimeTypes.Concat(
            new[]
            {
                "application/javascript",
                "application/json;",
                "application/xml",
                "text/css",
                "text/html",
                "text/json",
                "text/plain",
                "text/xml",
                "text/javascript",
                "font/woff2",
            });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

//Cors allow all (required for remote traces)
builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
}));
//Logging
string Log4NetFile = "log4net.config";
builder.Services.AddLogging(builder => { builder.AddLog4Net(Log4NetFile); });
ILog _logger = LogManager.GetLogger("Startup");

//I build the app
var app = builder.Build();
app.UseRouting();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapRazorPages();
app.MapControllers();
// app.MapBlazorHub();
app.UseMiddleware<StoreServerURLFilter>();
app.UseStatusCodePages();

//Cors allow all (required for remote traces)
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment() || true)
{
    app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");
    app.UseExceptionHandler(new ExceptionHandlerOptions()
    {
        AllowStatusCode404Response = true,
        ExceptionHandlingPath = "/error"
    });
    app.UseHsts();
}

//Proxy Forwarding IP Address
var forwardingOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardingOptions.KnownNetworks.Clear(); // Loopback by default, this should be temporary
forwardingOptions.KnownProxies.Clear(); // Update to include
app.UseForwardedHeaders(forwardingOptions);

//WebMarkupMin
app.UseResponseCompression();
app.UseWebMarkupMin();
app.UseStaticFiles(
    new StaticFileOptions()
    {
        OnPrepareResponse =
            r =>
            {
                string? path = r.File.PhysicalPath;
                if (!string.IsNullOrEmpty(path) &&
                    (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".gif") || path.EndsWith(".jpg") || path.EndsWith(".png") || path.EndsWith(".svg") || path.EndsWith(".webp") || path.EndsWith(".woff2")))
                {
                    r.Context.Response.Headers.Append("Cache-Control", "max-age=31536000");
                }
            }
    });

//I record the starting events
app.Lifetime.ApplicationStarted.Register(() => { _logger.Info($"Application started, environment: {builder.Environment.EnvironmentName}"); });
app.Lifetime.ApplicationStopping.Register(() => { _logger.Info("Application stopping"); });
app.Lifetime.ApplicationStopped.Register(() => { _logger.Info("Application ended"); });

//I run the app
await app.RunAsync();