using System;
using System.Collections;
using System.Collections.Generic;
using BeatSaberDataProvider;
using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.DataProviders;
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
            SongHashDataProvider songHashes = new SongHashDataProvider();
            songHashes.Initialize();
            PlayerDataProvider playerData = new PlayerDataProvider();
            playerData.Initialize();
            string fileRead = File.ReadAllText("BeatSaverSongsTest.json");
            //var songList = JsonConvert.DeserializeObject<List<Song>>(fileRead);
            var songList = JToken.Parse(fileRead);
            var listSongs = new List<Song>();
            SongDataContext context = new SongDataContext();
            context.Database.EnsureCreated();
            Song jSong = null;
            foreach (var item in songList.Children())
            {
                jSong = Song.CreateFromJson(item);
                context.Songs.Update(jSong);
                context.SaveChanges();
                listSongs.Add(jSong);
            }
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
