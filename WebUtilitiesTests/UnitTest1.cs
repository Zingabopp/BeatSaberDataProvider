using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace WebUtilitiesTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var thing = Path.GetFullPath("MockClasses");
            var thing2 = Path.GetFullPath(@"MockClasses\");
            var thing3 = new DirectoryInfo(@"MockClasses");
            var thing4 = new DirectoryInfo(@"MockClasses\");
            var thing5 = thing3.Parent.GetDirectories();
        }
    }
}
