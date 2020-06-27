using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.DownloadContainers;
using WebUtilities.HttpClientWrapper;

namespace WebUtilitiesTests.HttpClientDownloadWithProgressTests
{
    [TestClass]
    public class HttpClientDownloadWithProgress_Tests
    {
        private static readonly string TestOutputPath = Path.GetFullPath(@"Output\HttpClientDownloadWithProgressTests");
        private static readonly IWebClient Client = new HttpClientWrapper();

        [TestMethod]
        public async Task CustomStream_Canceled()
        {
            Uri uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            CancellationTokenSource cts = new CancellationTokenSource(1500);
            Directory.CreateDirectory(TestOutputPath);
            string filePath = Path.Combine(TestOutputPath, "CustomStream.iso");
            DownloadContainer downloadContainer = new MemoryDownloadContainer();
            Progress<DownloadProgress> progressHandler = new Progress<DownloadProgress>(p =>
            {
                Console.WriteLine($"IProgress: {p.TotalBytesDownloaded}/{p.TotalDownloadSize} : {p.ProgressPercent}%");
            });
            try
            {
                var response = await Client.GetAsync(uri, cts.Token);
                await downloadContainer.ReceiveDataAsync(response.Content, true, progressHandler, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Wrong exception thrown: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
