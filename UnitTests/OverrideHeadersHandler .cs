using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
    public class OverrideHeadersHandler : DelegatingHandler
    {
        private readonly Dictionary<string, string> _headersToOverride;

        public OverrideHeadersHandler(Dictionary<string, string> headersToOverride)
        {
            _headersToOverride = headersToOverride;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Real request
            var response = await base.SendAsync(request, cancellationToken);

            // Header overriding
            foreach (var header in _headersToOverride)
            {
                response.Headers.Remove(header.Key);
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return response;
        }
    }
}
