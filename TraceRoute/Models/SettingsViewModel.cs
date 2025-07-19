namespace TraceRoute.Models
{
    /// <summary>
    /// The information stored in the appsettings.json file.
    /// </summary>
    public class SettingsViewModel
    {
        public bool EnableRemoteTraces { get; set; }
        public bool HostRemoteTraces { get; set; }
        public string RootNode { get; set; } = "";
        public string CurrentServerURL { get; set; } = "";
        public string ServerLocation { get; set; } = "";
    }
}
