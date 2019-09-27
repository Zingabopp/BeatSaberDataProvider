using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using WebUtilities;
using WebUtilities.HttpClientWrapper;
using Newtonsoft.Json.Linq;
using System;

namespace SongFeedReadersTests.MockClasses.MockTests
{
    [TestClass]
    public class MockWebClientTests
    {
        [TestMethod]
        public void GetAsync_PageNotFound()
        {
            using (var mockClient = new MockWebClient())
            using (var realClient = new HttpClientWrapper())
            {
                mockClient.Timeout = 500;
                realClient.Timeout = 500;
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
