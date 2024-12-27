using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Helpers;

namespace UnitTests.Helpers
{
    [TestClass]
    public class TraceHelperTests
    {
        [TestMethod]
        public async Task TraceRoute()
        {
            // Arrange
            string destination = "www.google.com";
            // Act
            List<string> result = await TraceHelper.TraceRoute(destination);
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);            

            // Windows
            result =  TraceHelper.TraceRouteWindows(destination).ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);

            // Linux
            result = await TraceHelper.TraceRouteLinux(destination);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }
    }
}
