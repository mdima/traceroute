using Bunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Components.Molecules
{
    [TestClass]
    public class AboutTests : Bunit.TestContext
    {

        [TestMethod]
        public void TestAbout()
        {
            // Arrange a simple render
            var cut = RenderComponent<TraceRoute.Components.Molecules.About>();
            Assert.IsNotNull(cut);
            // Check if the header contains the expected text
            Assert.Contains(cut.Instance.currentVersion!.Major + ".", cut.Markup);

            // Null assembly
            System.Reflection.Assembly.SetEntryAssembly(null);
            cut = RenderComponent<TraceRoute.Components.Molecules.About>();
            Assert.Contains("<span>Unknown</span>", cut.Markup);

            // I set the current version to null
            cut.Instance.currentVersion = null;
            cut.Render();
            Assert.Contains("<span>Unknown</span>", cut.Markup);
        }
    }
}
