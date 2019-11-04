using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using WebUtilities;
using WebUtilities.HttpClientWrapper;
using WebUtilities.WebWrapper;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace WebUtilitiesTests.IWebClientTests
{
    [TestClass]
    public class GetAsync_Tests
    {
        private static readonly string TestOutputPath = Path.GetFullPath(@"Output\IWebClientTests");

        [TestMethod]
        public void Success_NullContentLength()
        {
            var client1 = new WebClientWrapper();
            var client2 = new HttpClientWrapper();
            var url = "http://beatsaver.com/";
            bool expectedResponseSuccess = true;
            int expectedStatus = 200;
            string expectedContentType = "text/html";
            long? expectedContentLength = null;
            bool? actualResponseSuccess = null;
            int? actualStatus = null;
            string actualContentType = null;
            long? actualContentLength = null;
            Uri actualRequestUri = null;
            var action = new Func<IWebClient, Task<IWebResponseMessage>>(async (target) =>
            {
                Exception exception = null;
                try
                {
                    using (var response = await target.GetAsync(url).ConfigureAwait(false))
                    {
                        actualResponseSuccess = response.IsSuccessStatusCode;
                        actualStatus = response.StatusCode;
                        actualContentType = response.Content.ContentType;
                        actualContentLength = response.Content.ContentLength;
                        actualRequestUri = response.RequestUri;
                        //Assert.Fail("Should've thrown exception");
                    }
                }
                catch (WebClientException ex)
                {
                    Assert.Fail("Should not have thrown exception");
                    exception = ex;
                    actualResponseSuccess = ex.Response?.IsSuccessStatusCode;
                    actualStatus = ex.Response.StatusCode;
                    actualRequestUri = ex.Response.RequestUri;
                }
                Assert.AreEqual(expectedResponseSuccess, actualResponseSuccess);
                Assert.AreEqual(expectedStatus, actualStatus);
                Assert.AreEqual(expectedContentType, actualContentType);
                Assert.AreEqual(expectedContentLength, actualContentLength);
                Assert.AreEqual(url, actualRequestUri.ToString());
                if (exception != null)
                    throw exception;
                return null;
            });
            CompareGetAsync(client1, client2, action);
        }

        [TestMethod]
        public void NotFound()
        {
            var client1 = new WebClientWrapper();
            var client2 = new HttpClientWrapper();
            var url = "https://beatsaver.com/cdn/5317/aaa14fb7dcaeda7a688db77617045a24d7baa151d.zip";
            bool expectedResponseSuccess = false;
            int expectedStatus = 404;
            string expectedContentType = null;
            long? expectedContentLength = null;
            bool? actualResponseSuccess = null;
            string actualReasonPhrase = null;
            int? actualStatus = null;
            string actualContentType = null;
            long? actualContentLength = null;
            Uri actualRequestUri = null;
            var action = new Func<IWebClient, Task<IWebResponseMessage>>(async (target) =>
            {
                IWebResponseMessage response = null;
                Exception exception = null;
                try
                {
                    response = await target.GetAsync(url).ConfigureAwait(false);
                    actualResponseSuccess = response.IsSuccessStatusCode;
                    actualStatus = response.StatusCode;
                    actualContentType = response.Content.ContentType;
                    actualContentLength = response.Content.ContentLength;
                    actualRequestUri = response.RequestUri;
                    Assert.Fail("Should've thrown exception");
                }
                catch (WebClientException ex)
                {
                    exception = ex;
                    actualResponseSuccess = ex.Response?.IsSuccessStatusCode;
                    actualReasonPhrase = ex.Response.ReasonPhrase;
                    actualStatus = ex.Response.StatusCode;
                    actualRequestUri = ex.Response.RequestUri;
                }
                Assert.AreEqual(expectedResponseSuccess, actualResponseSuccess);
                Assert.AreEqual(expectedStatus, actualStatus);
                Assert.AreEqual(expectedContentType, response?.Content?.ContentType);
                Assert.AreEqual(expectedContentLength, response?.Content?.ContentLength);
                Assert.AreEqual(url, actualRequestUri.ToString());
                if (exception != null)
                    throw exception;
                return response;
            });
            CompareGetAsync(client1, client2, action);
        }

        [TestMethod]
        public void UsingWrapped_NotFound()
        {
            var client1 = new WebClientWrapper();
            client1.ErrorHandling = ErrorHandling.ReturnEmptyContent;
            var client2 = new HttpClientWrapper();
            var url = "https://beatsaver.com/cdn/5317/aaa14fb7dcaeda7a688db77617045a24d7baa151d.zip";
            bool expectedResponseSuccess = false;
            int expectedStatus = 404;
            string expectedContentType = null;
            long? expectedContentLength = null;
            bool? actualResponseSuccess = null;
            int? actualStatus = null;
            string actualContentType = null;
            long? actualContentLength = null;
            Uri actualRequestUri = null;
            var action = new Func<IWebClient, Task<IWebResponseMessage>>(async (target) =>
            {
                IWebResponseMessage response = null;
                Exception exception = null;
                try
                {
                    using (response = await target.GetAsync(url).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }
                catch (WebClientException ex)
                {
                    exception = ex;
                    actualResponseSuccess = ex.Response?.IsSuccessStatusCode;
                    actualStatus = ex.Response.StatusCode;
                    actualRequestUri = ex.Response.RequestUri;
                }
                Assert.AreEqual(expectedResponseSuccess, actualResponseSuccess);
                Assert.AreEqual(expectedStatus, actualStatus);
                Assert.AreEqual(expectedContentType, response?.Content?.ContentType);
                Assert.AreEqual(expectedContentLength, response?.Content?.ContentLength);
                Assert.AreEqual(url, actualRequestUri.ToString());
                if (exception != null)
                    throw exception;
                return response;
            });
            CompareGetAsync(client1, client2, action);
        }

        [TestMethod]
        public void Canceled()
        {
            var client1 = new WebClientWrapper();
            var client2 = new HttpClientWrapper();
            var cts = new CancellationTokenSource(500);
            var url = "http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso";
            string directory = Path.Combine(TestOutputPath, "Canceled");
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, "canceled.iso");
            bool expectedResponseSuccess = false;
            int expectedStatus = 404;
            string expectedContentType = "text/plain";
            long? expectedContentLength = 14;
            bool? actualResponseSuccess = null;
            int? actualStatus = null;
            string actualContentType = null;
            long? actualContentLength = null;
            Uri actualRequestUri = null;
            var action = new Func<IWebClient, Task<IWebResponseMessage>>(async (target) =>
            {
                IWebResponseMessage response = null;
                Exception exception = null;
                try
                {
                    response = await target.GetAsync(url, cts.Token).ConfigureAwait(false);
                    await response.Content.ReadAsFileAsync(filePath, true, cts.Token);
                    Assert.Fail("Should've thrown exception");
                }
                catch (WebClientException ex)
                {
                    exception = ex;
                    actualResponseSuccess = ex.Response?.IsSuccessStatusCode;
                    actualStatus = ex.Response.StatusCode;
                    actualRequestUri = ex.Response.RequestUri;
                }
                Assert.AreEqual(expectedResponseSuccess, actualResponseSuccess);
                Assert.AreEqual(expectedStatus, actualStatus);
                Assert.AreEqual(expectedContentType, response?.Content?.ContentType);
                Assert.AreEqual(expectedContentLength, response?.Content?.ContentLength);
                Assert.AreEqual(url, actualRequestUri.ToString());
                if (exception != null)
                    throw exception;
                return response;
            });
            CompareGetAsync(client1, client2, action);
        }

        [TestMethod]
        public void Timeout()
        {
            var client1 = new WebClientWrapper();
            client1.Timeout = 1;
            var client2 = new HttpClientWrapper();
            client2.Timeout = 1;
            var url = "http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-desktop-amd64.iso";
            string directory = Path.Combine(TestOutputPath, "Canceled");
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, "canceled.iso");
            bool expectedResponseSuccess = false;
            int expectedStatus = 408;
            string expectedContentType = null;
            long? expectedContentLength = null;
            bool? actualResponseSuccess = null;
            int? actualStatus = null;
            string actualContentType = null;
            long? actualContentLength = null;
            Uri actualRequestUri = null;
            var action = new Func<IWebClient, Task<IWebResponseMessage>>(async (target) =>
            {
                IWebResponseMessage response = null;
                Exception exception = null;
                try
                {
                    response = await target.GetAsync(url).ConfigureAwait(false);
                    Assert.Fail("Should've thrown exception");
                }
                catch (WebClientException ex)
                {
                    exception = ex;
                    actualResponseSuccess = ex.Response?.IsSuccessStatusCode;
                    actualStatus = ex.Response.StatusCode;
                    actualRequestUri = ex.Response.RequestUri;
                }
                Assert.AreEqual(expectedResponseSuccess, actualResponseSuccess);
                Assert.AreEqual(expectedStatus, actualStatus);
                Assert.AreEqual(expectedContentType, response?.Content?.ContentType);
                Assert.AreEqual(expectedContentLength, response?.Content?.ContentLength);
                Assert.AreEqual(url, actualRequestUri.ToString());
                if (exception != null)
                    throw exception;
                return response;
            });
            CompareGetAsync(client1, client2, action);
        }




        public static void CompareGetAsync(IWebClient client1, IWebClient client2, Func<IWebClient, Task<IWebResponseMessage>> action)
        {
            IWebResponseMessage response1 = null;
            IWebResponseMessage response2 = null;
            Exception client1Exception = null;
            Exception client2Exception = null;
            try
            {
                response1 = action(client1).Result;
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch (AggregateException ex)
            {
                var assertFailedException = ex.InnerExceptions.Where(e => e is AssertFailedException).FirstOrDefault();
                if (assertFailedException != null)
                    throw assertFailedException;
                if (ex.InnerExceptions.Count == 1)
                    client1Exception = ex.InnerException;
                else
                    client1Exception = ex;
            }
            catch (Exception ex)
            {
                client1Exception = ex;
            }

            try
            {
                response2 = action(client2).Result;
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch (AggregateException ex)
            {
                var assertFailedException = ex.InnerExceptions.Where(e => e is AssertFailedException).FirstOrDefault();
                if (assertFailedException != null)
                    throw assertFailedException;
                if (ex.InnerExceptions.Count == 1)
                    client2Exception = ex.InnerException;
                else
                    client2Exception = ex;
            }
            catch (Exception ex)
            {
                client2Exception = ex;
            }

            if (client1Exception != null || client2Exception != null)
            {
                Console.WriteLine($"Client1Exception: {client1Exception?.GetType().Name}: {client1Exception?.Message}");
                Console.WriteLine($"Client2Exception: {client2Exception?.GetType().Name}: {client2Exception?.Message}");
                Assert.AreEqual(client1Exception?.GetType(), client2Exception?.GetType());
                if (client1Exception is WebClientException webException1 && client2Exception is WebClientException webException2)
                {
                    Assert.AreEqual(webException1.Uri?.ToString(), webException2.Uri?.ToString());
                    CompareResponses(webException1.Response, webException2.Response);
                }
                else if(client1Exception is OperationCanceledException || client2Exception is OperationCanceledException)
                {
                    if (client1Exception is OperationCanceledException && client2Exception is OperationCanceledException)
                        Console.WriteLine($"Operation canceled");
                    else
                        Assert.Fail($"One client was canceled, the other wasn't.");
                }
                else
                {
                    Assert.Fail("Not a WebClientException");
                }
                return;
            }
            CompareResponses(response1, response2);
            response1?.Dispose();
            response2?.Dispose();
        }

        public static void CompareResponses(IWebResponseMessage response1, IWebResponseMessage response2)
        {
            if (response1 == null || response2 == null)
            {
                if(!(response1 == null && response2 == null))
                    Assert.Fail($"One response is null and the other isn't.");
            }
            else
            {
                Assert.AreEqual(response1.IsSuccessStatusCode, response2.IsSuccessStatusCode);
                Console.WriteLine($"IsSuccessStatusCode is {response1.IsSuccessStatusCode}");
                Assert.AreEqual(response1.StatusCode, response2.StatusCode);
                Console.WriteLine($"Status code is {response1.StatusCode}");
                Assert.AreEqual(response1.ReasonPhrase, response2.ReasonPhrase);
                Console.WriteLine($"ReasonPhrase is {response1.ReasonPhrase}");
                //Assert.AreEqual(response1.Exception?.GetType(), response2.Exception?.GetType());
                Assert.AreEqual(response1.RequestUri.ToString(), response2.RequestUri.ToString());
                Console.WriteLine($"RequestUri is {response1.RequestUri}");

                CompareContent(response1.Content, response2.Content);
            }
        }

        public static void CompareResponses(FaultedResponse response1, FaultedResponse response2)
        {
            if (response1 == null)
            {
                Assert.IsNull(response2, $"response1 is null, but {response2.GetType().Name} is not.");
            }
            else
            {
                Assert.AreEqual(response1.IsSuccessStatusCode, response2.IsSuccessStatusCode);
                Console.WriteLine($"IsSuccessStatusCode is {response1.IsSuccessStatusCode}");
                Assert.AreEqual(response1.StatusCode, response2.StatusCode);
                Console.WriteLine($"Status code is {response1.StatusCode}");
                Assert.AreEqual(response1.ReasonPhrase, response2.ReasonPhrase);
                Console.WriteLine($"ReasonPhrase is {response1.ReasonPhrase}");
                //Assert.AreEqual(response1.Exception?.GetType(), response2.Exception?.GetType());
                Assert.AreEqual(response1.RequestUri.ToString(), response2.RequestUri.ToString());
                Console.WriteLine($"RequestUri is {response1.RequestUri}");
            }
        }

        public static void CompareContent(IWebResponseContent content1, IWebResponseContent content2)
        {
            if(content1 == null || content2 == null)
            {
                Assert.IsNull(content1);
                Assert.IsNull(content2);
                return;
            }
            Assert.AreEqual(content1.ContentType, content2.ContentType);
            if (string.IsNullOrEmpty(content1.ContentType))
                Console.WriteLine("ContentType is null or empty");
            else
                Console.WriteLine($"ContentType is {content1.ContentType}");
            Assert.AreEqual(content1.ContentLength, content2.ContentLength);
            if (content1.ContentLength == null)
                Console.WriteLine("Content length is null");
            else
                Console.Write($"ContentLength is {content1.ContentLength}");
        }
    }
}
