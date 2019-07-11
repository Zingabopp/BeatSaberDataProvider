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
            ScrapedDataProvider.Initialize();
            //SongHashDataProvider songHashes = new SongHashDataProvider();
            //songHashes.Initialize();
            //PlayerDataProvider playerData = new PlayerDataProvider();
            //playerData.Initialize();

            SongDataContext context = new SongDataContext() { EnableSensitiveDataLogging = true, UseLoggerFactory = true };
            //context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            string fileRead = File.ReadAllText("BeatSaverTestSongs.json");
            //var songList = JsonConvert.DeserializeObject<List<Song>>(fileRead);
            var songList = JToken.Parse(fileRead)["docs"];
            var listSongs = new List<Song>();
            int count = 0;
            foreach (var jSong in songList.Children())
            {
                var newSong = Song.CreateFromJson(jSong);
                listSongs.Add(newSong);
                context.Update(newSong);
                context.SaveChanges();
                //var newSong = Song.CreateFromJson(jSong);
                //context.Add(Song.CreateFromJson(jSong));
                //context.SaveChanges();
                count++;
            }
            
            
            return;
            var songs = ScrapedDataProvider.Songs;


            context.Songs.UpdateRange(listSongs);
            context.SaveChanges();

            ScrapedDataProvider.TryGetSongByHash("2FDDB136BDA7F9E29B4CB6621D6D8E0F8A43B126", out Song song);
            ScrapedDataProvider.TryGetSongByKey("b", out Song believer);
            string hash = believer.Hash.ToLower();
            ScrapedDataProvider.BeatSaverSongs.WriteFile();
        }
    }
}
