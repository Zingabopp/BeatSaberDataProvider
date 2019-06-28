using BeatSaberDataProvider.DataModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeatSaberDataProvider.DataProviders
{
    [Serializable]
    public class SongHashDataProvider
    {
        public FileInfo CurrentFile;
        public static readonly string DEFAULT_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low", @"Hyperbolic Magnetism\Beat Saber");
        //public static readonly string BACKUP_FOLDER = Path.Combine(DEFAULT_FOLDER, "SongHashDataBackups");
        public const string DEFAULT_FILE_NAME = "SongHashData.dat";
        public Dictionary<string, SongHashData> Data;
        public SongHashDataProvider()
        {
            Data = new Dictionary<string, SongHashData>();
        }

        /// <summary>
        /// Parse the SongHashData file into the 'Data' Dictionary. If no file path is provided it uses the default path.
        /// </summary>
        /// <param name="filePath"></param>
        public void Initialize(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = Path.Combine(DEFAULT_FOLDER, DEFAULT_FILE_NAME);
            if (File.Exists(filePath))
            {
                var str = File.ReadAllText(filePath);
                //JsonConvert.PopulateObject(str, Data);
                var token = JObject.Parse(str);
                foreach (JProperty item in token.Children())
                {
                    var directory = item.Name;
                    Data.Add(directory, new SongHashData(item, directory));
                }
                CurrentFile = new FileInfo(filePath);
            }
            foreach (var item in Data.Keys)
            {
                Data[item].Directory = item;
            }
        }

        public void AddMissingHashes(string CustomLevelsFolder = "")
        {
            DirectoryInfo songFolder = null;
            if (string.IsNullOrEmpty(CustomLevelsFolder))
            {
                string path = string.Empty;
                if (Data.Count > 0)
                    path = Data.First().Key;
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException("Custom songs folder wasn't provided and can't be determined from the SongHashData file.");
                CustomLevelsFolder = path;
            }
            songFolder = new DirectoryInfo(CustomLevelsFolder).Parent;
            var missingHashData = new Dictionary<string, SongHashData>();
            foreach (var folder in songFolder.GetDirectories())
            {
                if (Data.Keys.Any(k => k == folder.FullName))
                    continue;
                if(folder.GetFiles().Any(f => f.Name.ToLower() == "info.dat"))
                {
                    missingHashData.Add(folder.FullName, new SongHashData() { Directory = folder.FullName });
                }
            }
            missingHashData.Values.ToList().AsParallel().ForAll(h => h.GenerateHash());
            foreach (var item in missingHashData)
            {
                Data.Add(item.Key, item.Value);
            }
        }

        public SongHashData AddSongToHash(string songDirectory, bool hashImmediately = true)
        {
            if (Directory.Exists(songDirectory))
            {
                var newSongHashData = new SongHashData() { Directory = songDirectory };
                if (hashImmediately)
                    newSongHashData.GenerateHash();
                Data.Add(songDirectory, newSongHashData);
                return newSongHashData;
            }
            return null;
        }

        public void AddSongsToHash(string[] songDirectories)
        {
            var hashDataList = new List<SongHashData>();
            foreach (var songDirectory in songDirectories)
            {
                hashDataList.Add(AddSongToHash(songDirectory, false));
            }
            hashDataList.AsParallel().ForAll(h => h.GenerateHash());
        }


    }


}
