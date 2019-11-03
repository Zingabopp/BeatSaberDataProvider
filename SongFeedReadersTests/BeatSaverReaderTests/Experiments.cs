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

namespace SongFeedReadersTests.Experiments
{
    [TestClass]
    public class GetSongsFromFeed_Tests
    {
        static GetSongsFromFeed_Tests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void AdvancedSearch()
        {
            var url = @"https://beatsaver.com/api/search/advanced?q=uploaded:[2019-01-01 TO 2019-01-02] AND metadata.difficulties.easy:true AND metadata.songName:*e*";
            var response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            var songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url);
            url = @"https://beatsaver.com/api/search/advanced?q=uploaded:[2019-01-01 TO 2019-01-02] AND metadata.difficulties.easy:true AND metadata.songName:e";
            response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url);
            url = @"https://beatsaver.com/api/search/advanced?q=metadata.songName:bomb";
            response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url);
            url = @"https://beatsaver.com/api/search/text/?q=bomb";
            response = WebUtils.GetBeatSaverAsync(new Uri(url), CancellationToken.None).Result;
            songs = BeatSaverReader.ParseSongsFromPage(response.Content.ReadAsStringAsync().Result, url);
        }

    }
}
