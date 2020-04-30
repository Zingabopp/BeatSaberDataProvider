using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using WebUtilities;
using WebUtilities.HttpClientWrapper;
using WebUtilities.WebWrapper;
using Newtonsoft.Json.Linq;
using System;

namespace SongFeedReadersTests.MockClasses.MockTests
{
    [TestClass]
    public class MockWebClientTests
    {
        [TestMethod]
        public void HttpClient_GetAsync_PageNotFound()
        {
            using (var mockClient = new MockWebClient())
            using (var realClient = new HttpClientWrapper())
            {
                mockClient.Timeout = 5000;
                realClient.Timeout = 5000;
                realClient.ErrorHandling = ErrorHandling.ReturnEmptyContent;
                var testUrl = new Uri("https://bsaber.com/wp-jsoasdfn/bsabasdfer-api/songs/");
                //WebUtils.Initialize(realClient);
                using (var realResponse = realClient.GetAsync(testUrl).Result)
                using (var mockResponse = mockClient.GetAsync(testUrl).Result)
                {
                    var test = realResponse?.Content?.ReadAsStringAsync().Result;
                    Assert.AreEqual(realResponse.IsSuccessStatusCode, mockResponse.IsSuccessStatusCode);
                    Assert.AreEqual(realResponse.StatusCode, mockResponse.StatusCode);
                    Assert.AreEqual(realResponse.Content?.ContentType, mockResponse.Content.ContentType);
                }
            }
        }

        [TestMethod]
        public void WebClient_GetAsync_PageNotFound()
        {
            using (var mockClient = new MockWebClient())
            using (var realClient = new WebClientWrapper())
            {
                mockClient.Timeout = 5000;
                realClient.Timeout = 5000;
                realClient.ErrorHandling = ErrorHandling.ReturnEmptyContent;
                var testUrl = new Uri("https://bsaber.com/wp-jsoasdfn/bsabasdfer-api/songs/");
                //WebUtils.Initialize(realClient);
                using (var realResponse = realClient.GetAsync(testUrl).Result)
                using (var mockResponse = mockClient.GetAsync(testUrl).Result)
                {
                    var test = realResponse?.Content?.ReadAsStringAsync().Result;
                    Assert.AreEqual(realResponse.IsSuccessStatusCode, mockResponse.IsSuccessStatusCode);
                    Assert.AreEqual(realResponse.StatusCode, mockResponse.StatusCode);
                    Assert.AreEqual(realResponse.Content?.ContentType, mockResponse.Content.ContentType);
                }
            }
        }
    }
}
