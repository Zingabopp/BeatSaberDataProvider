using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using static SongFeedReaders.WebUtils;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReadersTests.WebUtilsTests
{
    [TestClass]
    public class GetBeatSaver_Tests
    {
        static GetBeatSaver_Tests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void ParseBeatSaverRateLimit_Test()
        {
            string pageKey = "{PAGE}";
            string baseUrl = $"https://beatsaver.com/api/search/advanced/{pageKey}/?q=uploaded:[2019-01-01%20TO%202019-01-06]";
            Uri uri = new Uri(baseUrl.Replace(pageKey, "0"));
            using (var resp = GetBeatSaverAsync(uri, CancellationToken.None).Result)
            {
                var rateLimit = ParseBeatSaverRateLimit(resp.Headers);
                Assert.IsNotNull(rateLimit);
                Assert.IsTrue(rateLimit.TimeToReset > DateTime.Now);
            }
        }

        [TestMethod]
        public void ExceedsRateLimit()
        {
            string pageKey = "{PAGE}";
            string baseUrl = $"https://beatsaver.com/api/search/advanced/{pageKey}/?q=uploaded:[2019-01-01%20TO%202019-01-06]";
            var uriList = new List<Tuple<int, Uri>>();
            for (int i = 0; i < 30; i++)
            {
                uriList.Add(new Tuple<int, Uri>(i, new Uri(baseUrl.Replace(pageKey, i.ToString()))));
            }
            var responses = uriList.AsParallel().Select(uri =>
            {
                Task.Delay(uri.Item1 * 5).Wait();
                var retStr = $"Page {uri.Item1}: ";
                try
                {
                    using (var resp = GetBeatSaverAsync(uri.Item2, CancellationToken.None).Result)
                    {
                        var rateLimit = ParseBeatSaverRateLimit(resp.Headers);
                        retStr += $"{rateLimit.CallsRemaining} calls for {(rateLimit.TimeToReset - DateTime.Now).TotalSeconds} ({rateLimit.TimeToReset})";
                        Assert.IsNotNull(rateLimit);
                    }
                }
                catch (Exception ex)
                {
                    retStr += ex.Message;
                }
                return retStr;
            }).ToList();
            foreach (var resp in responses)
            {
                Console.WriteLine(resp);
            }
        }

    }
}
