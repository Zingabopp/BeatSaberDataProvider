using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSaberDataProvider;
using System.Collections;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BeatSaberDataProvider.DataProviders;
using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.Util;
using System;

namespace BeatSaberDataProviderTest
{
    [TestClass]
    public class SongHashData_Tests
    {
        [TestMethod]
        public void GenerateDirectoryHash_Test()
        {
            string testDir = @"C:\Program Files (x86)\Steam\steamapps\common\Beat Saber\Beat Saber_Data\CustomLevels\1001-675 Party Rock Anthem";
            var testSong = new SongHashData() { Directory = testDir };
            testSong.GenerateDirectoryHash();
            testSong.GenerateHash();
            Console.WriteLine($"testSong hash: {testSong.DirectoryHash}");
            var testSong2 = new SongHashData() { Directory = testDir };
            testSong2.GenerateDirectoryHash();
            testSong2.GenerateHash();
            Console.WriteLine($"testSong2 hash: {testSong2.DirectoryHash}");
            Assert.AreEqual(testSong.DirectoryHash, testSong2.DirectoryHash);
        }


    }
}
