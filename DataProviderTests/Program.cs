using BeatSaberDataProvider;
using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.DataProviders;
using BeatSaberDataProvider.Util;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static BeatSaberDataProvider.Util.DatabaseExtensions;

namespace DataProviderTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing...");
            //ScrapedDataProvider.Initialize();
            //SongHashDataProvider songHashes = new SongHashDataProvider();
            //songHashes.Initialize();
            //PlayerDataProvider playerData = new PlayerDataProvider();
            //playerData.Initialize();
            //ScrapedDataProvider.ScoreSaberSongs.Initialize("ScoreSaberScrape.json");

            //var songs = ScrapedDataProvider.Songs.SelectMany(s => s.Value.ScoreSaberDifficulties).GroupBy(d => d.DifficultyName);
            //var topSongs = ScrapedDataProvider.Songs.Values
            //    .OrderByDescending(s => s.ScoreSaberDifficulties.Aggregate(0, (a, b) => a + b.ScoresPerDay))
            //    .Select(s => $"{s.KeyAsInt} {s.SongName} - Uploaded: {s.Uploaded.ToShortDateString()}, Plays in past 24 hours: {s.ScoreSaberDifficulties.Aggregate(0, (a, b) => a + b.ScoresPerDay)}");
            //foreach (var item in ScrapedDataProvider.BeatSaverSongs.Data)
            //{
            //    var test = new Song(item, ScrapedDataProvider.ScoreSaberSongs.Data.Where(d => d.SongHash == item.hash));
            //    //Console.WriteLine($"{item.Key}: {item.Count()}");
            //}
            //var rankedMaul = ssScrape.Where(d => d.Ranked && d.DifficultyName.ToLower().Contains("dm")).ToList();
            SongDataContext context = new SongDataContext() { EnableSensitiveDataLogging = false, UseLoggerFactory = false };
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.LoadQuery(context.Songs.Where(s => true)).Load();
            //string fileRead = File.ReadAllText("BeatSaverTestSongs.json");
            //string fileRead = File.ReadAllText("BeatSaverTestSongsUpdate.json");
            //var songList = JToken.Parse(fileRead)["docs"];
            string fileRead = File.ReadAllText(@"ScrapedData\BeatSaverScrape.json");
            var songList = JToken.Parse(fileRead);


            var listSongs = new List<Song>();
            int count = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var jSong in songList.Children())
            {
                var newSong = Song.CreateFromJson(jSong);
                //listSongs.Add(newSong);
                context.Add(newSong);
                //context.SaveChanges();
                //var newSong = Song.CreateFromJson(jSong);
                //context.Add(Song.CreateFromJson(jSong));
                //context.SaveChanges();
                count++;
            }
            Console.WriteLine($"-----Song processing took {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            //context.AddRange(listSongs);
            context.SaveChanges();
            Console.WriteLine($"-----Database save took {sw.Elapsed.TotalSeconds}");
            return;
            //var songs = ScrapedDataProvider.Songs;


            //context.Songs.UpdateRange(listSongs);
            //context.SaveChanges();

            //ScrapedDataProvider.TryGetSongByHash("2FDDB136BDA7F9E29B4CB6621D6D8E0F8A43B126", out Song song);
            //ScrapedDataProvider.TryGetSongByKey("b", out Song believer);
            //string hash = believer.Hash.ToLower();
            //ScrapedDataProvider.BeatSaverSongs.WriteFile();
        }
    }
}
