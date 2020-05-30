using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongFeedReaders.Data;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReadersTests.WebUtilsTests
{
    [TestClass]
    public class SongInfoProviderTests
    {
        static SongInfoProviderTests()
        {
            TestSetup.Initialize();
        }
        public static SongInfoManager GetDefaultManager()
        {
            SongInfoManager? manager = new SongInfoManager();
            manager.AddProvider<BeatSaverSongInfoProvider>();
            manager.AddProvider<MockInfoProvider>("MockInfoProvoder", 50);
            return manager;
        }
        [TestMethod]
        public async Task GetByKeyFromMock()
        {
            SongInfoManager manager = GetDefaultManager();
            string key = "ac78";
            SongInfoResponse response = await manager.GetSongByKeyAsync(key).ConfigureAwait(false);
            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.Source is MockInfoProvider);
            Assert.IsNotNull(response.Song);
            Assert.AreEqual(0, response.GetFailedResponses().Length);
        }

        [TestMethod]
        public async Task GetByKeyNotInMock()
        {
            SongInfoManager manager = GetDefaultManager();
            string key = "b";
            SongInfoResponse response = await manager.GetSongByKeyAsync(key).ConfigureAwait(false);
            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.Source is BeatSaverSongInfoProvider);
            Assert.IsNotNull(response.Song);
            Assert.AreEqual(1, response.GetFailedResponses().Length);
        }

        [TestMethod]
        public async Task GetNonexistantKey()
        {
            SongInfoManager manager = GetDefaultManager();
            string key = "9d11a8ed";
            SongInfoResponse response = await manager.GetSongByKeyAsync(key).ConfigureAwait(false);
            Assert.IsFalse(response.Success);
            Assert.IsNull(response.Song);
            Assert.AreEqual(2, response.GetFailedResponses().Length);
        }

        [TestMethod]
        public async Task GetByHashFromMock()
        {
            SongInfoManager manager = GetDefaultManager();
            string hash = "25170877f7b500369be0c2d1ffbdc8c6d1ad4227";
            SongInfoResponse response = await manager.GetSongByHashAsync(hash).ConfigureAwait(false);
            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.Source is MockInfoProvider);
            Assert.IsNotNull(response.Song);
            Assert.AreEqual(0, response.GetFailedResponses().Length);
        }

        [TestMethod]
        public async Task GetByHashNotInMock()
        {
            SongInfoManager manager = GetDefaultManager();
            string hash = "9d11a002f1d2b6b08dce90a3e2b4a62dab62b8ed";
            SongInfoResponse response = await manager.GetSongByHashAsync(hash).ConfigureAwait(false);
            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.Source is BeatSaverSongInfoProvider);
            Assert.IsNotNull(response.Song);
            Assert.AreEqual(1, response.GetFailedResponses().Length);
        }

        [TestMethod]
        public async Task GetNonexistantHash()
        {
            SongInfoManager manager = GetDefaultManager();
            string hash = "9d11a002f1d2b6b08dce90a32dab62b8ed";
            SongInfoResponse response = await manager.GetSongByHashAsync(hash).ConfigureAwait(false);
            Assert.IsFalse(response.Success);
            Assert.IsNull(response.Song);
            Assert.AreEqual(2, response.GetFailedResponses().Length);
        }
    }

    public class MockInfoProvider : SongInfoProvider
    {
        public static ScrapedSong[] Songs;
        static MockInfoProvider()
        {
            string pageText = File.ReadAllText(@"Data\BeatSaverListPage.json");
            Uri? uri = null;
            Songs = BeatSaverReader.ParseSongsFromPage(pageText, uri, true).ToArray();
        }

        public override bool Available { get; } = true;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<ScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _ = hash ?? throw new ArgumentNullException(nameof(hash));
            return Songs.Where(s => hash.Equals(s?.Hash, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<ScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            return Songs.Where(s => key.Equals(s?.Key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }
    }
}
