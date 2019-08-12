﻿using BeatSaberDataProvider.DataModels;
using BeatSaberDataProvider.DataProviders;
using BeatSaberDataProvider.Util;
using BeatSaberDataProvider.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BeatSaberDataProvider
{
    public static class ScrapedDataProvider
    {
        public static bool Initialized { get; private set; }
        public static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly DirectoryInfo DATA_DIRECTORY = new DirectoryInfo(Path.Combine(ASSEMBLY_PATH, "ScrapedData"));

        public static BeatSaverScrape BeatSaverSongs { get; set; }
        public static ScoreSaberScrape ScoreSaberSongs { get; set; }
        public static Dictionary<string, Song> Songs { get; set; }

        public static void Initialize()
        {
            if (!DATA_DIRECTORY.Exists)
                DATA_DIRECTORY.Create();
            DATA_DIRECTORY.Refresh();
            BeatSaverSongs = new BeatSaverScrape();
            BeatSaverSongs.Initialize();
            ScoreSaberSongs = new ScoreSaberScrape();
            ScoreSaberSongs.Initialize();
            Songs = new Dictionary<string, Song>();
            //foreach (var song in BeatSaverSongs.Data)
            //{
            //    var newSong = new Song(song.Hash) {
            //        BeatSaverInfo = song
            //    };
            //    if (Songs.AddOrUpdate(song.Hash, newSong))
            //        Logger.Warning($"Repeated hash while creating SongInfo Dictionary, this should not happen. {song.name} by {song.metadata.levelAuthorName}");
            //}
            foreach (var diff in ScoreSaberSongs.Data)
            {
                if (diff.SongHash.Count() < 40)
                    continue; // Using the old hash, skip
                
                if (Songs.ContainsKey(diff.SongHash))
                    Songs[diff.SongHash].ScoreSaberDifficulties.AddOrUpdate(diff);
                else
                {
                    var songMatch = BeatSaverSongs.Data.Where(s => s.hash == diff.SongHash).FirstOrDefault();
                    if (songMatch != null)
                    {
                        var newSong = new Song(songMatch) { ScoreSaberDifficulties = new List<ScoreSaberDifficulty>() };
                        diff.Song = newSong;
                        newSong.ScoreSaberDifficulties.AddOrUpdate(diff);
                        Songs.Add(diff.SongHash, newSong);
                    }
                }
            }
            Initialized = true;
        }

        public static List<Song> ReadScrapedFile(string filePath)
        {
            List<Song> results = new List<Song>();

            if (File.Exists(filePath))
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    results = (List<Song>) serializer.Deserialize(file, typeof(List<Song>));
                }

            return results;
        }

        /// <summary>
        /// Attempts to find a song with the provided hash. If there's no matching
        /// song in the ScrapedData and searchOnline is true, it searches Beat Saver. If a match is found
        /// online, it adds the SongInfo to the ScrapedData. Returns true if a SongInfo is found.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="song"></param>
        /// <param name="searchOnline"></param>
        /// <returns></returns>
        [Obsolete("Need to make this Async")]
        public static bool TryGetSongByHash(string hash, out Song song, bool searchOnline = true)
        {
            hash = hash.ToUpper();
            song = Songs.ContainsKey(hash) ? Songs[hash] : null;
            if (song == null && searchOnline)
            {
                Logger.Info($"Song with hash: {hash}, not in scraped data, searching Beat Saver...");
                song = OnlineSongSearch.GetSongByHashAsync(hash).Result;
                if (song != null)
                {
                    song.ScrapedAt = DateTime.Now;
                    TryAddToScrapedData(song);
                }
                else
                    Logger.Warning($"Unable to find song with hash {hash} on Beat Saver, skipping.");
            }

            return song != null;
        }

        /// <summary>
        /// Attempts to find a song with the provided Beat Saver song ID. If there's no matching
        /// song in the ScrapedData and searchOnline is true, it searches Beat Saver. If a match is found
        /// online, it adds the SongInfo to the ScrapedData. Returns true if a SongInfo is found.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="song"></param>
        /// <param name="searchOnline"></param>
        /// <returns></returns>
        [Obsolete("Need to make this Async")]
        public static bool TryGetSongByKey(string key, out Song song, bool searchOnline = true)
        {
            key = key.ToLower();
            song = Songs.Values.Where(s => s.Key == key).FirstOrDefault();
            if (song == null && searchOnline)
            {
                Logger.Info($"Song with key: {key}, not in scraped data, searching Beat Saver...");
                song = OnlineSongSearch.GetSongByKeyAsync(key).Result;
                if (song != null)
                {
                    
                    TryAddToScrapedData(song);
                }
                else
                    Logger.Warning($"Unable to find song with key {key} on Beat Saver, skipping.");
            }

            return song != null;
        }

        /// <summary>
        /// Adds the provided SongInfo to the ScrapedData if song isn't already in there.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static bool TryAddToScrapedData(Song song)
        {
            if (Songs.Values.Where(s => s.Hash == song.Hash).Count() == 0)
            {
                //Logger.Debug($"Adding song {song.key} - {song.songName} by {song.authorName} to ScrapedData");
                lock (Songs)
                {
                    Songs.Add(song.Hash, song);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to find a SongInfo matching the provided SongInfoEnhanced.
        /// It creates a new SongInfo, attaches the provided SongInfoEnhanced, and adds it to the ScrapedData if no match is found.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="searchOnline"></param>
        /// <returns></returns>
        public static Song GetOrCreateSong(Song song)//, bool searchOnline = true)
        {
            bool foundOnline = TryGetSongByHash(song.Hash, out Song songInfo, false);
            //if (foundOnline)
            //
            //BeatSaverSongs.AddOrUpdate(song);
            //}
            //if (songInfo == null)
            //{
            //    songInfo = new Song(song);
            //    TryAddToScrapedData(songInfo);
            //}
            // songInfo = ;
            return songInfo;
        }

        //[Obsolete("Maybe don't use this.")]
        //public static Song GetOrCreateSong(ScoreSaberDifficulty song, bool searchOnline = true)
        //{
        //    bool foundOnline = TryGetSongByHash(song.SongHash, out Song songInfo, searchOnline);
        //    if (foundOnline)
        //    {
        //        BeatSaverSongs.AddOrUpdate(songInfo.BeatSaverInfo);
        //    }
        //    ScoreSaberSongs.AddOrUpdate(song);
        //    if (songInfo == null)
        //    {
        //        songInfo = song.GenerateSongInfo();
        //        TryAddToScrapedData(songInfo);
        //    }
        //    songInfo.ScoreSaberInfo.AddOrUpdate(song.uid, song);
        //    return songInfo;
        //}

        public static Song GetSong(ScoreSaberDifficulty song, bool searchOnline = true)
        {
            bool foundOnline = TryGetSongByHash(song.SongHash, out Song songInfo, searchOnline);
            if (songInfo != null)
                songInfo.ScoreSaberDifficulties.AddOrUpdate(song);
            return songInfo;
        }

        public static void BuildDatabaseFromJson(string databasePath, string beatSaverScrapePath, string scoreSaberPath)
        {
            var dbFile = new FileInfo(databasePath);
            var bsScrapeFile = new FileInfo(beatSaverScrapePath);
            var ssScrapeFile = new FileInfo(scoreSaberPath);
            dbFile.Directory.Create();
            using (var context = new SongDataContext(dbFile.FullName))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                JToken songList = null;
                var serializer = new JsonSerializer();
                var listSongs = new List<Song>();
                int count = 0;
                Stopwatch sw = new Stopwatch();

                List<ScoreSaberDifficulty> ssDiffs = null;
                using (var fs = new FileStream(ssScrapeFile.FullName, FileMode.Open))
                using (var sr = new StreamReader(fs))
                using (var jsonTextReader = new JsonTextReader(sr))
                {

                    //songList = JToken.Parse(sr);
                    ssDiffs = serializer.Deserialize<List<ScoreSaberDifficulty>>(jsonTextReader);
                }

                if (count == 0)
                {
                    using (var fs = new FileStream(bsScrapeFile.FullName, FileMode.Open))
                    using (var sr = new StreamReader(fs))
                    using (var jsonTextReader = new JsonTextReader(sr))
                    {

                        //songList = JToken.Parse(sr);
                        songList = serializer.Deserialize<JToken>(jsonTextReader);
                    }
                    sw.Start();
                    foreach (var jSong in songList.Children())
                    {
                        var newSong = Song.CreateFromJson(jSong);
                        newSong.ScoreSaberDifficulties = new List<ScoreSaberDifficulty>();
                        var matchedSSdiffs = ssDiffs.Where(d => d.SongHash == newSong.Hash);
                        foreach (var diff in matchedSSdiffs)
                        {
                            newSong.ScoreSaberDifficulties.Add(diff);
                        }
                        //listSongs.Add(newSong);
                        context.Add(newSong);
                        //context.SaveChanges();
                        //var newSong = Song.CreateFromJson(jSong);
                        //context.Add(Song.CreateFromJson(jSong));
                        //context.SaveChanges();
                        count++;
                    }
                }
                context.SaveChanges();
            }

        }
    }
    // From: https://stackoverflow.com/questions/43747477/how-to-parse-huge-json-file-as-stream-in-json-net?rq=1
    public static class JsonReaderExtensions
    {
        public static IEnumerable<T> SelectTokensWithRegex<T>(
            this JsonReader jsonReader, Regex regex)
        {
            JsonSerializer serializer = new JsonSerializer();
            while (jsonReader.Read())
            {
                if (regex.IsMatch(jsonReader.Path)
                    && jsonReader.TokenType != JsonToken.PropertyName)
                {
                    yield return serializer.Deserialize<T>(jsonReader);
                }
            }
        }
    }
}
