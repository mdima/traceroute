using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Services;

namespace UnitTests.Services
{

    public class BogonIPServiceTests
    {
        private BogonIPService _BogonIPService;

        public BogonIPServiceTests()
        {
            NullLoggerFactory factory = new();
            _BogonIPService = new BogonIPService(factory);
        }

        [Fact]
        public void IsBogonIP()
        {
            bool result = _BogonIPService.IsBogonIP("127.0.0.1");
            Assert.True(result);

            result = _BogonIPService.IsBogonIP("192.168.0.1");
            Assert.True(result);

            result = _BogonIPService.IsBogonIP("192.188.248.215");
            Assert.False(result);

            result = _BogonIPService.IsBogonIP("errorip");
            Assert.False(result);
        }

        [Fact]
        public async Task IsPrivateServer()
        {
            bool result = await _BogonIPService.IsPrivateServer("127.0.0.1");
            Assert.True(result);

            result = result = await _BogonIPService.IsPrivateServer("http://localhost:8081");
            Assert.True(result);

            result = result = await _BogonIPService.IsPrivateServer("http://traceroute.michelecasa");
            Assert.True(result);

            result = await _BogonIPService.IsPrivateServer("192.188.248.215");
            Assert.False(result);

            result = await _BogonIPService.IsPrivateServer("https://traceroute.nt2.it/");
            Assert.False(result);

            result = await _BogonIPService.IsPrivateServer("wrongIpAddress");
            Assert.True(result);

        }
    }
}
