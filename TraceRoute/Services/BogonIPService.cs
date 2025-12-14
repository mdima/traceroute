using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Policy;
using TraceRoute.Controllers;

namespace TraceRoute.Services
{
    /// <summary>
    /// Used to know if the given IP Address is a Bogon (private) network address
    /// </summary>
    public class BogonIPService
    {
        private readonly List<IPNetwork2> _BogonNetworks;
        private readonly ILogger _logger;

        /// <summary>
        /// Initialize the Bogon Networks
        /// </summary>
        /// <param name="LoggerFactory"></param>
        public BogonIPService(ILoggerFactory LoggerFactory)
        {
            _logger = LoggerFactory.CreateLogger<APIController>();
            _BogonNetworks =
            [
                // IPv4
                IPNetwork2.Parse("0.0.0.0/8"),
                IPNetwork2.Parse("10.0.0.0/8"),
                IPNetwork2.Parse("100.64.0.0/10"),
                IPNetwork2.Parse("127.0.0.0/8"),
                IPNetwork2.Parse("169.254.0.0/16"),
                IPNetwork2.Parse("172.16.0.0/12"),
                IPNetwork2.Parse("192.0.0.0/24"),
                IPNetwork2.Parse("192.0.2.0/24"),
                IPNetwork2.Parse("192.168.0.0/16"),
                IPNetwork2.Parse("198.18.0.0/15"),
                IPNetwork2.Parse("198.51.100.0/24"),
                IPNetwork2.Parse("203.0.113.0/24"),
                IPNetwork2.Parse("224.0.0.0/4"),
                IPNetwork2.Parse("240.0.0.0/4"),
                IPNetwork2.Parse("255.255.255.255/32"),
                // IPv6
                IPNetwork2.Parse("::/128"),
                IPNetwork2.Parse("::1/128"),
                IPNetwork2.Parse("::ffff:0:0/96"),
                IPNetwork2.Parse("::/96"),
                IPNetwork2.Parse("100::/64"),
                IPNetwork2.Parse("2001:10::/28"),
                IPNetwork2.Parse("2001:db8::/32"),
                IPNetwork2.Parse("fc00::/7"),
                IPNetwork2.Parse("fe80::/10"),
                IPNetwork2.Parse("fec0::/10"),
                IPNetwork2.Parse("ff00::/8"),
                // 6to4
                IPNetwork2.Parse("2002::/24"),
                IPNetwork2.Parse("2002:a00::/24"),
                IPNetwork2.Parse("2002:7f00::/24"),
                IPNetwork2.Parse("2002:a9fe::/32"),
                IPNetwork2.Parse("2002:ac10::/28"),
                IPNetwork2.Parse("2002:c000::/40"),
                IPNetwork2.Parse("2002:c000:200::/40"),
                IPNetwork2.Parse("2002:c0a8::/32"),
                IPNetwork2.Parse("2002:c612::/31"),
                IPNetwork2.Parse("2002:c633:6400::/40"),
                IPNetwork2.Parse("2002:cb00:7100::/40"),
                IPNetwork2.Parse("2002:e000::/20"),
                IPNetwork2.Parse("2002:f000::/20"),
                IPNetwork2.Parse("2002:ffff:ffff::/48"),
                // Teredo
                IPNetwork2.Parse("2001::/40"),
                IPNetwork2.Parse("2001:0:a00::/40"),
                IPNetwork2.Parse("2001:0:7f00::/40"),
                IPNetwork2.Parse("2001:0:a9fe::/48"),
                IPNetwork2.Parse("2001:0:ac10::/44"),
                IPNetwork2.Parse("2001:0:c000::/56"),
                IPNetwork2.Parse("2001:0:c000:200::/56"),
                IPNetwork2.Parse("2001:0:c0a8::/48"),
                IPNetwork2.Parse("2001:0:c612::/47"),
                IPNetwork2.Parse("2001:0:c633:6400::/56"),
                IPNetwork2.Parse("2001:0:cb00:7100::/56"),
                IPNetwork2.Parse("2001:0:e000::/36"),
                IPNetwork2.Parse("2001:0:f000::/36"),
                IPNetwork2.Parse("2001:0:ffff:ffff::/64"),
            ];
        }

        /// <summary>
        /// Provides a method to check if the given IP Address is a Bogon (private) network address.
        /// </summary>
        /// <param name="iPAddress">The IP address to check</param>
        /// <returns>TRUE if it is a Bogon IP address</returns>
        public bool IsBogonIP(string iPAddress)
        {
            if (iPAddress == "...") return false;

            if (IPAddress.TryParse(iPAddress, out IPAddress? parsedIPAddress)) {
                foreach (IPNetwork2 network in _BogonNetworks)
                {
                    if (network.Contains(parsedIPAddress)) return true;
                }
                return false;
            }
            else
            {
                _logger.LogWarning("Cannot parse the IP Adderss {0}", iPAddress);
                return false;
            }
        }

        /// <summary>
        /// Provides a method to check if the given URL belongs to a private non reachable server.
        /// </summary>
        /// <param name="URLAddress">The URL address to check</param>
        /// <returns>TRUE if it is a private URL Address</returns>
        public async Task<bool> IsPrivateServer(string URLAddress)
        {
            try
            {
                IPAddress[] addresses;
                if (IPAddress.TryParse(URLAddress, out IPAddress? parsedIPAddress))
                {
                    addresses = [parsedIPAddress];
                }
                else
                {
                    var uri = new Uri(URLAddress);
                    addresses = await Dns.GetHostAddressesAsync(uri.Host);
                }

                foreach (var ip in addresses)
                {
                    if (IsBogonIP(ip.ToString()))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in IsPrivateServer. URL Address {0}", URLAddress);
                return true;
            }
        }
    }
}