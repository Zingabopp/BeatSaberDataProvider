using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.HttpClientWrapper;

namespace WebUtilitiesTests.HttpClientWrapperTests
{
    [TestClass]
    public class WebClientContent_Tests
    {
        private static readonly string TestOutputPath = Path.GetFullPath(@"Output\HttpClientContent");

        [TestMethod]
        public void CanceledDownload()
        {
            IWebClient client = new HttpClientWrapper();
            var uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            var cts = new CancellationTokenSource(500);
            string directory = Path.Combine(TestOutputPath, "CanceledDownload");
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, "CanceledDownload.iso");
            
            using(var response = client.GetAsync(uri, cts.Token).Result)
            {
                if (!response.IsSuccessStatusCode)
                    Assert.Fail($"Error getting response from {uri}: {response.ReasonPhrase}");
                try
                {
                    var thing = response.Content.ReadAsFileAsync(filePath, true, cts.Token).Result;
                    Assert.Fail("Didn't throw exception");
                }
                catch(AggregateException ex)
                {
                    if (!(ex.InnerException is OperationCanceledException))
                        Assert.Fail("Wrong exception thrown.");
                }
                catch (Exception)
                {
                    Assert.Fail("Wrong exception thrown.");
                }
            }
            cts.Dispose();
            client.Dispose();
        }
    }
}
