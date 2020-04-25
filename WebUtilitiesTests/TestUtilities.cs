using System;
using System.Collections.Generic;
using System.Text;
using WebUtilities;

namespace WebUtilitiesTests
{
    public static class TestUtilities
    {
        public static IWebClient GetWebClient()
        {
            var client = new WebUtilities.WebWrapper.WebClientWrapper();
            client.SetUserAgent("WebUtilitiesTests/1.0.0");
            return client;
        }
        public static IWebClient GetHttpClient()
        {
            var client = new WebUtilities.HttpClientWrapper.HttpClientWrapper();
            client.SetUserAgent("WebUtilitiesTests/1.0.0");
            return client;
        }
    }
}
