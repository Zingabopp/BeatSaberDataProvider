using BeatSaberDataProvider.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BeatSaberDataProvider.DataProviders
{
    [Obsolete("Replacing with separate classes for Beat Saver and ScoreSaber data")]
    public class SyncSaberScrape : IScrapedDataModel<List<Song>, Song>
    {
        private static readonly string ASSEMBLY_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public SyncSaberScrape()
        {
            Data = new List<Song>();
            DefaultPath = Path.Combine(DATA_DIRECTORY.FullName, "SyncSaberScrapedData.json");
        }

        public override void Initialize(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = DefaultPath;
            Data = new List<Song>();
            //(filePath).Populate(this);
            if(File.Exists(filePath))
                ReadScrapedFile(filePath).Populate(this);
            //JsonSerializer serializer = new JsonSerializer();
            //if (test.Type == Newtonsoft.Json.Linq.JTokenType.Array)
            //    Data = test.ToObject<List<SongInfo>>();
            Initialized = true;
            CurrentFile = new FileInfo(filePath);
        }


        public override void WriteFile(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = CurrentFile.FullName;
            using (StreamWriter file = File.CreateText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this);
            }
        }
    }
}
