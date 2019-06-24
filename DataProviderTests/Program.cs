using System;
using System.Collections;
using System.Collections.Generic;
using BeatSaberDataProvider;
using BeatSaberDataProvider.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace DataProviderTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing...");
            ScrapedDataProvider.Initialize();
            string fileRead = File.ReadAllText("BeatSaverSongsTest.json");
            var songList = JsonConvert.DeserializeObject<List<Song>>(fileRead);
            var songs = ScrapedDataProvider.Songs;
            ScrapedDataProvider.TryGetSongByHash("2FDDB136BDA7F9E29B4CB6621D6D8E0F8A43B126", out Song song);
            ScrapedDataProvider.TryGetSongByKey("b", out Song believer);
            string hash = believer.Hash.ToLower();
            ScrapedDataProvider.BeatSaverSongs.WriteFile();
        }
    }
}
