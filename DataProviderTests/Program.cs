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
using BeatSaberDataProvider.Util;
using System.Diagnostics;

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
            ScoreSaberDifficulty ssDiff = null;
            
            SongDataContext context = new SongDataContext();
            //context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            //var roContext = new SongDataContext();
            //var fullQuery = roContext.Songs
            //    .Include(s => s.SongDifficulties)
            //        .ThenInclude(sd => sd.Difficulty) //IntelliSense doesn't work here
            //    .Include(s => s.BeatmapCharacteristics)
            //        .ThenInclude(bc => bc.Characteristic)
            //    .Include(s => s.ScoreSaberDifficulties)
            //    .Include(s => s.Uploader);
            //fullQuery.Load();
            var lazyContext = new SongDataContext();
            //var lazyLoadTest = lazyContext.Songs.Where(s => s.ScoreSaberDifficulties.Any(sd => sd.Ranked == true)).Take(10);
            var hashData = new SongHashDataProvider();
            hashData.Initialize();
            hashData.AddMissingHashes();

            Stopwatch timer = new Stopwatch();
            timer.Restart();
            hashData.Data.Values.ToList().AsParallel().ForAll(h => h.GenerateHash());
            timer.Stop();
            Console.WriteLine($"Hashed {hashData.Data.Count()} songs in {timer.Elapsed.ToString()}");
            int hashCount = 0;
            timer.Restart();
            foreach (var item in hashData.Data)
            {
                item.Value.GenerateHash();
                hashCount++;
            }
            timer.Stop();
            Console.WriteLine($"Hashed {hashCount} songs in {timer.Elapsed.ToString()}");
            
            var eb = PredicateBuilder.False<Song>();
            eb = eb.Or(s => s.Downloads < 50);
            eb = eb.Or(s => s.ScoreSaberDifficulties.Any(d => d.Ranked == true));
            var ebTest = lazyContext.Songs
                .Include(s => s.ScoreSaberDifficulties)
                .Include(s => s.BeatmapCharacteristics)
                    .ThenInclude(bc => bc.Characteristic)
                .Where(eb).OrderBy(s => s.BeatsPerMinute).Take(10).ToList();
            var query =
                from c in lazyContext.Songs
                let ssds = lazyContext.ScoreSaberDifficulties.Where(d => d.SongHash == c.Hash)
                where ssds.Any(sd => sd.Ranked == true)
                select c;
            var qTest = query.OrderByDescending(s => s.Uploaded).Take(20).ToList();
            var testQuery = lazyContext.Songs.Select(s => s);
            testQuery = testQuery.Where(s => s.BeatsPerMinute > 100 && s.BeatsPerMinute < 150);
            testQuery = testQuery.Where(s => s.ScoreSaberDifficulties.Any(d => d.Ranked));
            testQuery = testQuery.Where(s => s.SongDifficulties.Any(d => d.Difficulty.DifficultyName == "ExpertPlus"));
            var testExpression = testQuery.Expression.ToString();
            
            //context.Songs.Load();
            //context.Difficulties.Load();
            //context.Characteristics.Load();
            //context.ScoreSaberDifficulties.Load();
            //context.BeatmapCharacteristics.Load();

            //string beatSaverSongs = File.ReadAllText("BeatSaverTestSongs.json");
            string beatSaverSongs = File.ReadAllText("BeatSaverScrape.json");
            JToken bsSongsJson = JToken.Parse(beatSaverSongs);//["docs"];
            List<Song> bsSongs = new List<Song>();
            List<ScoreSaberDifficulty> ssDiffs = new List<ScoreSaberDifficulty>();
            //string ssDiffsJson = File.ReadAllText("ScoreSaberTestDifficulties.json");
            string ssDiffsJson = File.ReadAllText("ScoreSaberScrape.json");
            JToken ssDiffsToken = JToken.Parse(ssDiffsJson);//["songs"];
            foreach (var item in ssDiffsToken.Children())
            {
                ssDiff = new ScoreSaberDifficulty(item);
                context.AddOrUpdate(ssDiff);
                //ssDiffs.Add(ssDiff);
            }
            context.SaveChanges();

            foreach (var item in bsSongsJson.Children())
            {
                jSong = Song.CreateFromJson(item);
                context.AddOrUpdate(jSong);
                //try
                //{
                //    context.SaveChanges();
                //}catch(DbUpdateConcurrencyException ex)
                //{
                //    var entry = ex.Entries.Single();
                //    var something = entry.CurrentValues;
                //    entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                //}
                bsSongs.Add(jSong);
            }
            var authors = bsSongs.Select(s => s.Uploader);
            var duplicates = authors.GroupBy(a => a.UploaderName).Where(g => g.Count() > 1).Select(g => (g.Count(), g.Key));

            context.SaveChanges();
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
