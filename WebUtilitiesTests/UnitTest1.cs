using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

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

        [TestMethod]
        public void AggregateExceptionTest()
        {
            var mainTask = Task.Run(() =>
            {
                var a = Task.Run(() => { throw new InvalidOperationException("a"); });
                var b = Task.Run(() => { throw new ArgumentException("b"); });
                var c = Task.Run(() => { throw new ApplicationException("c"); });
                Task.WhenAll(a, b, c).Wait();
                Assert.Fail("Breaks on the WhenAll");
                throw new IOException("mainTask");
            });

            try
            {
                mainTask.Wait();
                Assert.Fail("Should have thrown exception");
            }
            catch (AggregateException ex)
            {
                ex = ex.Flatten();
                Console.WriteLine(ex.InnerExceptions.Count);
            }
        }
    }
}
