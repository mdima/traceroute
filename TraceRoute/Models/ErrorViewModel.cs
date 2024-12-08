using System;

namespace TraceRoute.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}