using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSaberDataProvider;
using System.Collections;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BeatSaberDataProvider.DataProviders;
using BeatSaberDataProvider.DataModels;
using Microsoft.EntityFrameworkCore;
using BeatSaberDataProvider.Util;

namespace BeatSaberDataProviderTest
{
    [TestClass]
    public class SongDataContext_Tests
    {
        [TestMethod]
        public void AddingSongs_Test()
        {
            using (SongDataContext context = new SongDataContext("songAddTest.db"))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                string json = File.ReadAllText(@"TestData\BeatSaverTestSongs.json");
                var songList = JToken.Parse(json);
                foreach (var jSong in songList["docs"].Children())
                {
                    var newSong = Song.CreateFromJson(jSong);
                    context.AddOrUpdate(newSong);
                }
                context.SaveChanges();
            }
            using (SongDataContext context = new SongDataContext("songAddTest.db"))
            {
                context.LoadQuery(context.Songs.Where(s => true)).Load();
                var song = context.Songs.Find("5d2662f993d6020006079bf8");
                Assert.IsTrue(song != null);
                Assert.IsTrue(song.Uploader.UploaderName == "snail0fd3ath");
                Assert.IsTrue(song.Key == "5658");
                Assert.IsTrue(song.Downloads == 3);
                Assert.IsTrue(song.BeatmapCharacteristics.Count == 2);
                var songChar = song.BeatmapCharacteristics.Where(bc => bc.CharacteristicName == "Standard").Single();
                Assert.IsTrue(songChar != null);
                Assert.IsTrue(songChar.CharacteristicDifficulties.Count == 3);
                var charDiff = songChar.CharacteristicDifficulties.Where(d => d.DifficultyLevel == 3).Single();
                Assert.IsTrue(charDiff != null);
                Assert.IsTrue(charDiff.Difficulty == "Hard");
                Assert.IsTrue(charDiff.Bombs == 15);

            }
        }
        [TestMethod]
        public void UpdatingSongs_Test()
        {
            SongDataContext context = new SongDataContext("songUpdateTest.db");
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            string json = File.ReadAllText(@"TestData\BeatSaverListWithConverted.json");
            var songList = JToken.Parse(json);
            foreach (var jSong in songList["docs"].Children())
            {
                var newSong = Song.CreateFromJson(jSong);
                context.AddOrUpdate(newSong);
            }
            context.SaveChanges();
            var convertedGroup = context.Songs.Local.GroupBy(s => s.Converted);
        }
    }
}
