using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using SongFeedReaders.Readers.BeatSaver;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReadersTests
{
    [TestClass]
    public class Experiments
    {
        static Experiments()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void AdvancedSearch()
        {
            var url = @"https://beatsaver.com/api/search/advanced?q=uploaded:[2019-01-01 TO 2019-01-02] AND metadata.difficulties.easy:true AND metadata.songName:*e*";
            var response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            var songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url, true);
            url = @"https://beatsaver.com/api/search/advanced?q=uploaded:[2019-01-01 TO 2019-01-02] AND metadata.difficulties.easy:true AND metadata.songName:e";
            response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url, true);
            url = @"https://beatsaver.com/api/search/advanced?q=metadata.songName:bomb";
            response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url, true);
            url = @"https://beatsaver.com/api/search/text/?q=bomb";
            response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url, true);
        }

        [TestMethod]
        public void AggregateExceptionTest()
        {
            var mainTask = Task.Run(async () =>
            {
                var a = Task.Run(() => { throw new InvalidOperationException("a"); });
                var b = Task.Run(() => { throw new ArgumentException("b"); });
                var c = Task.Run(() => { throw new ApplicationException("c"); });
                await Task.WhenAll(a, b, c);
                Assert.Fail("Breaks on the WhenAll");
                throw new IOException("mainTask");
            });

            try
            {
                mainTask.Wait();
                Assert.Fail("Should have thrown exception");
            }
            catch (AggregateException ex)
            {
                ex = ex.Flatten();
                Console.WriteLine(ex.InnerExceptions.Count);
            }
        }

    }
}
