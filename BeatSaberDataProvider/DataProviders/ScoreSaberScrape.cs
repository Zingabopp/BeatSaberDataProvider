﻿using BeatSaberDataProvider.DataModels;
using System.Collections.Generic;
using System.IO;

namespace BeatSaberDataProvider.DataProviders
{
    public class ScoreSaberScrape : IScrapedDataModel<List<ScoreSaberDifficulty>, ScoreSaberDifficulty>
    {
        private readonly object dataLock = new object();
        public ScoreSaberScrape()
        {
            Initialized = false;
            DefaultPath = Path.Combine(DATA_DIRECTORY.FullName, "ScoreSaberScrape.json");
        }

        public override void Initialize(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = DefaultPath;
            Data = new List<ScoreSaberDifficulty>();
            //(filePath).Populate(this);
            if (File.Exists(filePath))
                ReadScrapedFile(filePath).Populate(Data);
            //JsonSerializer serializer = new JsonSerializer();
            //if (test.Type == Newtonsoft.Json.Linq.JTokenType.Array)
            //    Data = test.ToObject<List<SongInfo>>();
            Initialized = true;
            CurrentFile = new FileInfo(filePath);
        }
        /*
        public void AddOrUpdate(ScoreSaberSong newSong)
        {
            IEnumerable<ScoreSaberSong> existing = null;
            lock (dataLock)
            {
                existing = Data.Where(s => s.Equals(newSong));
            }
            if (existing.Count() > 1)
            {
                Logger.Warning("Duplicate hash in BeatSaverScrape, this shouldn't happen");
            }
            if (existing.SingleOrDefault() != null)
            {
                if (existing.Single().ScrapedAt < newSong.ScrapedAt)
                {
                    lock (dataLock)
                    {
                        Data.Remove(existing.Single());
                        Data.Add(newSong);
                    }
                }
            }
        }
        */
    }
}
