namespace TraceRoute.Models
{
    public class SettingsViewModel
    {
        public bool EnableRemoteTraces { get; set; }
        public bool HostRemoteTraces { get; set; }
        public string ServerId { get; set; } = "";
        public string RootNode { get; set; } = "";
        public string CurrentServerURL { get; set; } = "";
        public string ServerLocation { get; set; } = "";
    }
}
