using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using BeatSaberDataProvider;
using Microsoft.EntityFrameworkCore;
using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.DataProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
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
            Song jSong = null;
            
            
            SongDataContext context = new SongDataContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            
            context.Songs.Load();
            context.Difficulties.Load();
            context.Characteristics.Load();
            context.ScoreSaberDifficulties.Load();
            context.BeatmapCharacteristics.Load();
            
            string beatSaverSongs = File.ReadAllText("BeatSaverTestSongs.json");
            JToken bsSongsJson = JToken.Parse(beatSaverSongs)["docs"];
            List<Song> bsSongs = new List<Song>();

            foreach (var item in bsSongsJson.Children())
            {
                jSong = Song.CreateFromJson(item);
                context.AddOrUpdate(jSong);
                try
                {
                    context.SaveChanges();
                }catch(DbUpdateConcurrencyException ex)
                {
                    var entry = ex.Entries.Single();
                    var something = entry.CurrentValues;
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                }
                bsSongs.Add(jSong);
            }
            var testBMChar = new BeatmapCharacteristic() { SongId = "5d10e3663793fc0006d1e898", CharacteristicId = 1 };
            context.AddOrUpdate(testBMChar);
            beatSaverSongs = File.ReadAllText("BeatSaverTestSongsUpdate.json");
            bsSongsJson = JToken.Parse(beatSaverSongs)["docs"];
            foreach (var item in bsSongsJson.Children())
            {
                jSong = Song.CreateFromJson(item);
                //jSong.UpdateDB(ref context);
                //context.Songs.Update(jSong.UpdateDB(context));
                context.AddOrUpdate(jSong);
                context.SaveChanges();
                context.Songs.Remove(context.Songs.Find(jSong.PrimaryKey));
                try
                {
                    context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var entry = ex.Entries.Single();
                    var something = entry.CurrentValues;
                    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                }
                bsSongs.Add(jSong);
            }
            
            var testReadOnly = SongDataContext.AsReadOnly();
            testReadOnly.Songs.Load();
            string fileRead = File.ReadAllText("BeatSaverSongsTest.json");
            //var songList = JsonConvert.DeserializeObject<List<Song>>(fileRead);
            var songList = JToken.Parse(fileRead);
            var listSongs = new List<Song>();
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
