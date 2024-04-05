using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Helpers;

namespace UnitTests.Services
{
    [TestClass]
    public class BogonIPServiceTests
    {
        private BogonIPService _BogonIPService;

        public BogonIPServiceTests()
        {
            NullLoggerFactory factory = new();
            _BogonIPService = new BogonIPService(factory);
        }

        [TestMethod]
        public void IsBogonIP()
        {
            bool result = _BogonIPService.IsBogonIP("127.0.0.1");
            Assert.IsTrue(result);

            result = _BogonIPService.IsBogonIP("192.168.0.1");
            Assert.IsTrue(result);

            result = _BogonIPService.IsBogonIP("192.188.248.215");
            Assert.IsFalse(result);

            result = _BogonIPService.IsBogonIP("errorip");
            Assert.IsFalse(result);
        }
    }
}
