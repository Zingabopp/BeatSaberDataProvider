using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.HttpClientWrapper;

namespace WebUtilitiesTests.HttpClientDownloadWithProgressTests
{
    [TestClass]
    public class HttpClientDownloadWithProgress_Tests
    {
        private static readonly string TestOutputPath = Path.GetFullPath(@"Output\HttpClientDownloadWithProgressTests");

        [TestMethod]
        public void CanceledDownload()
        {
            IWebClient client = new HttpClientWrapper();
            var uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            var cts = new CancellationTokenSource(1500);
            string directory = Path.Combine(TestOutputPath, "CanceledDownload");
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, "CanceledDownload.iso");

            var progressDownload = new HttpClientDownloadWithProgress(client, uri, filePath);
            progressDownload.ProgressChanged += (long? totalFileSize, long totalBytesDownloaded, double? progressPercentage) =>
            {
                Console.WriteLine($"{totalBytesDownloaded}/{totalFileSize} : {progressPercentage}%");
            };

            try
            {
                progressDownload.StartDownload(cts.Token).Wait();
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerException is OperationCanceledException))
                    Assert.Fail("Wrong exception thrown.");
            }
            catch (Exception)
            {
                Assert.Fail("Wrong exception thrown.");
            }


        }
    }
}
