using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
namespace TraceRoute.Helpers
{
    public static class ConfigurationHelper
    {
        private static readonly string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        private readonly static IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets("TraceRouteSecrets")
            .Build();

        public static string GetGoogleMapsAPIKey()
        {
            return configuration["AppSettings:GoogleMapsAPIKey"]!;
        }
    }
}