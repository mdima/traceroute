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

        public static string GetServerID()
        {
            return GetAppSetting("ServerID");
        }

        public static bool SetServerID()
        {
            Guid guid = Guid.NewGuid();
            return SetAppSetting("ServerID", guid.ToString());
        }

        public static bool GetEnableRemoteTraces()
        {
            return GetAppSetting("EnableRemoteTraces", "true").ToLower() == "true";
        }

        public static bool SetEnableRemoteTraces(bool value)
        {
            return SetAppSetting<bool>("EnableRemoteTraces", value);
        }

        public static bool GetHostRemoteTraces() {
            return GetAppSetting("HostRemoteTraces", "false").ToLower() == "true";
        }

        public static bool SetHostRemoteTraces(bool value)
        {
            return SetAppSetting<bool>("HostRemoteTraces", value);
        }

        public static bool IsRootNode(HttpRequest request)
        {
            Uri rootURL = new Uri(GetRootNode());
            return request.Host.Host == rootURL.Host && request.Host.Port == rootURL.Port;
        }

        public static SettingsViewModel GetCurrentSettings(HttpRequest request)
        { 
            SettingsViewModel settings = new();

            if (request != null)
                settings.CurrentServerURL = request.Host.Value;
            settings.ServerId = ConfigurationHelper.GetServerID();
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

        public static bool SetAppSetting<T>(string key, T value)
        {
            try
            {
                if (value == null) return false;

                var configJson = File.ReadAllText("./config/appsettings.json");
                var attribs = File.GetAttributes("./config/appsettings.json");
                dynamic? currentConfig = JsonConvert.DeserializeObject(configJson);
                if (currentConfig == null) return false;
                if (currentConfig["AppSettings"] == null) return false;

                currentConfig["AppSettings"][key] = value;

                var updatedConfigJson = JsonConvert.SerializeObject(currentConfig, Formatting.Indented);
                File.WriteAllText("./config/appsettings.json", updatedConfigJson);
                configuration.Reload();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing app settings | {0}", ex.Message);
                return false;
            }
        }
    }
}
