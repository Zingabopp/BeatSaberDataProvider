using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SongFeedReaders.Readers.ScoreSaber;

namespace SongFeedReadersTests.ScoreSaberReaderTests
{
    [TestClass]
    public class GetSongsFromPageText_Tests
    {
        public static readonly string DataDir = Path.Combine("Data", "ScoreSaber");
        [TestMethod]
        public void ParseSongs()
        {
            string dataPath = Path.Combine(DataDir, "latest0.json");
            string dataStr = File.ReadAllText(dataPath);
            List<ScrapedSong>? songList = ScoreSaberFeed.GetSongsFromPageText(dataStr, "http://scoresaber.com/latest0.json", false);
            ScrapedSong song = songList[0];
        }
    }
}
