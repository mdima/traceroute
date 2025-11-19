using Bunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceRoute.Components.Molecules;
using static TraceRoute.Models.TraceResultViewModel;

namespace UnitTests.Components.Molecules
{

    public class IpDetailsComponentTests : BunitContext
    {
        [Fact]
        public void TestIpDetails()
        {
            // Arrange a simple render
            var cut = Render<IpDetailsComponent>(p =>
                p.Add(a => a.currentHop, null)
            );
            Assert.NotNull(cut);
            // Check if the header contains the expected text
            Assert.Contains("<span class=\"visually-hidden\">Loading...</span>", cut.Markup);

            // I set a valid hop 
            TraceHop traceHop = new TraceHop() { HopAddress = Guid.NewGuid().ToString(), Details = new() };
            cut = Render<IpDetailsComponent>(p =>
                            p.Add(a => a.currentHop, traceHop)
                        );
            Assert.Contains(traceHop.HopAddress, cut.Markup);
        }
    }
}
