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
        private static readonly IWebClient Client = new HttpClientWrapper();
        [TestMethod]
        public void NoFileOrStream()
        {
            Uri uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            DownloadWithProgress downloadWithProgress = Client.CreateDownloadWithProgress(uri);
            try
            {
                downloadWithProgress.StartDownload().Wait();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is InvalidOperationException expectedException)
                    Console.WriteLine($"{expectedException.GetType().Name}: {expectedException.Message}");
                else
                    Assert.Fail($"Wrong exception thrown: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Wrong exception thrown: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }

        [TestMethod]
        public async Task CustomStream_Canceled()
        {
            Uri uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            CancellationTokenSource cts = new CancellationTokenSource(1500);
            Directory.CreateDirectory(TestOutputPath);
            string filePath = Path.Combine(TestOutputPath, "CustomStream.iso");

            DownloadWithProgress progressDownload = Client.CreateDownloadWithProgress(uri);
            //progressDownload.ProgressChanged += (object sender, DownloadProgress progress) =>
            //{
            //    Console.WriteLine($"{progress.TotalBytesDownloaded}/{progress.TotalFileSize} : {progress.ProgressPercentage}%");
            //};
            Progress<DownloadProgress> progressHandler = new Progress<DownloadProgress>(p =>
            {
                Console.WriteLine($"IProgress: {p.TotalBytesDownloaded}/{p.TotalFileSize} : {p.ProgressPercentage}%");
            });

            try
            {
                using (FileStream stream = new FileStream(Path.Combine(TestOutputPath, "CustomStream.iso"), FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    await progressDownload.StartDownload(stream, progressHandler, cts.Token);
            }
            catch (OperationCanceledException ex)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Wrong exception thrown: {ex.GetType().Name}: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task CanceledDownload()
        {
            Uri uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            CancellationTokenSource cts = new CancellationTokenSource(1500);
            Directory.CreateDirectory(TestOutputPath);
            string filePath = Path.Combine(TestOutputPath, "CanceledDownload.iso");

            DownloadWithProgress progressDownload = new DownloadWithProgress(Client, uri, filePath);
            progressDownload.ProgressChanged += (object sender, DownloadProgress progress) =>
            {
                Console.WriteLine($"Event: {progress.TotalBytesDownloaded}/{progress.TotalFileSize} : {progress.ProgressPercentage}%");
            };
            Progress<DownloadProgress> progressHandler = new Progress<DownloadProgress>(p =>
            {
                Console.WriteLine($"IProgress: {p.TotalBytesDownloaded}/{p.TotalFileSize} : {p.ProgressPercentage}%");
            });

            try
            {
                await progressDownload.StartDownload(progressHandler, cts.Token);
            }
            catch (OperationCanceledException ex)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Wrong exception thrown.");
            }


        }
    }
}
