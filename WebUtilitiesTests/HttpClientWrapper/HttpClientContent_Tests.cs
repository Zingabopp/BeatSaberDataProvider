using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using WebUtilities;
using WebUtilities.HttpClientWrapper;
using WebUtilities.WebWrapper;

namespace WebUtilitiesTests.HttpClientWrapperTests
{
    [TestClass]
    public class WebClientContent_Tests
    {
        private static readonly string TestOutputPath = Path.GetFullPath(@"Output\WebClientContent_Tests");

        [TestMethod]
        public void CanceledDownload()
        {
            IWebClient client = new HttpClientWrapper();
            Uri uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            CancellationTokenSource cts = new CancellationTokenSource(700);
            Directory.CreateDirectory(TestOutputPath);
            string filePath = Path.Combine(TestOutputPath, "CanceledDownload.iso");

            using (IWebResponseMessage response = client.GetAsync(uri).Result)
            {
                if (!response.IsSuccessStatusCode)
                    Assert.Fail($"Error getting response from {uri}: {response.ReasonPhrase}");
                try
                {
                    CancellationTokenSource readCts = new CancellationTokenSource(100);
                    string thing = response.Content.ReadAsFileAsync(filePath, true, readCts.Token).Result;
                    Assert.Fail("Didn't throw exception");
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
            cts.Dispose();
            client.Dispose();
        }

        [TestMethod]
        public void CanceledBeforeResponse()
        {
            IWebClient client = new WebClientWrapper();
            Uri uri = new Uri("http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso");
            CancellationTokenSource cts = new CancellationTokenSource(100);
            Directory.CreateDirectory(TestOutputPath);
            string filePath = Path.Combine(TestOutputPath, "CanceledBeforeResponse.iso");
            try
            {
                using (IWebResponseMessage response = client.GetAsync(uri, cts.Token).Result)
                {
                    if (!response.IsSuccessStatusCode)
                        Assert.Fail($"Error getting response from {uri}: {response.ReasonPhrase}");
                    Assert.Fail("Didn't throw exception");

                }
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
            cts.Dispose();
            client.Dispose();
        }
    }
}
