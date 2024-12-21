using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TraceRoute.Models;

[assembly:InternalsVisibleTo("UnitTests")]
namespace TraceRoute.Helpers
{
    public static class ConfigurationHelper
    {
        private static readonly string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        private readonly static IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine("./config/", "appsettings.json"), optional: true, reloadOnChange: true)
            .Build();

        public static int GetCacheMinutes()
        {
            return (int)GetNumericValue("CacheMinutes", 60)!;
        }

        public static string GetRootNode()
        {
            return GetAppSetting("RootNode");
        }

        public static bool GetEnableRemoteTraces()
        {
            string? environmentValue = Environment.GetEnvironmentVariable("TRACEROUTE_ENABLEREMOTETRACES");

            if (environmentValue != null)
            {
                return environmentValue.ToLower() == "true";
            }
            else
            {
                return GetAppSetting("EnableRemoteTraces", "true").ToLower() == "true";
            }
        }

        public static bool GetHostRemoteTraces() {

            string? environmentValue = Environment.GetEnvironmentVariable("TRACEROUTE_HOSTREMOTETRACES");

            if (environmentValue != null)
            {
                return environmentValue.ToLower() == "true";
            }
            else
            {
                return GetAppSetting("HostRemoteTraces", "true").ToLower() == "true";
            }            
        }

        public static bool IsRootNode(HttpRequest request)
        {
            Uri rootURL = new Uri(GetRootNode());
            return request.Host.Host == rootURL.Host && request.Host.Port == rootURL.Port;
        }

        public static SettingsViewModel GetCurrentSettings(HttpRequest request)
        { 
            SettingsViewModel settings = new();

            if (request != null && request.Host.HasValue)
                settings.CurrentServerURL = request.Host.Value;            
            settings.HostRemoteTraces = ConfigurationHelper.GetHostRemoteTraces();
            settings.EnableRemoteTraces = ConfigurationHelper.GetEnableRemoteTraces();
            settings.RootNode = ConfigurationHelper.GetRootNode();

            return settings;
        }

        //Private functions
        private static int? GetNumericValue(string Key, int? DefaultValue = null)
        {
            if (int.TryParse(configuration[Key], out int Value))
            {
                return Value;
            }
            else
            {
                if (DefaultValue.HasValue)
                    return DefaultValue.Value;
                else
                    return null;
            }
        }

        private static string GetAppSetting(string Key, string DefaultValue = "")
        {
            string? Value = configuration["AppSettings:" + Key];
            if (Value != null)
            {
                return Value;
            }
            else
            {
                return DefaultValue;
            }
        }
    }
}
