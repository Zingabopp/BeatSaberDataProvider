using BeatSaberDataProvider.DataModels;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BeatSaberDataProvider.DataProviders
{
    public class SongHashDataProvider
    {
        private static readonly object _saveLock = new object();
        public static readonly string DEFAULT_SONGCORE_DATA_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low", @"Hyperbolic Magnetism\Beat Saber");
        public const string DEFAULT_SONGCORE_DATA_FILENAME = "SongHashData.dat";
        public const string DEFAULT_FOLDER = "SongHashData";
        public const string DEFAULT_FILE_NAME = "SongHashData.json";
        //public static readonly string BACKUP_FOLDER = Path.Combine(DEFAULT_FOLDER, "SongHashDataBackups");
        public static FileInfo SongHashFile { get; private set; }
        public static readonly ConcurrentDictionary<string, SongHashDataProvider> DataProviders = new ConcurrentDictionary<string, SongHashDataProvider>();

        public Dictionary<string, SongHashData> Data { get; private set; }

        public static void SetDataFile(string filePath)
        {
            lock (_saveLock)
            {
                var temp = new FileInfo(filePath);
                if (!temp.Exists)
                    temp.Create();
                SongHashFile = temp;
            }
        }

        public static async Task SaveChanges()
        {
            if (SongHashFile == null)
                throw new InvalidOperationException("Unable to save SongHashFile, data file not set.");
            bool writeFinished = false;
            var dataList = new Dictionary<string, SongHashData>();
            var keys = DataProviders.Keys.ToList();
            foreach (var key in keys)
            {
                var pairs = DataProviders[key].Data.ToList();
                foreach (var pair in pairs)
                {
                    dataList.Add(pair.Key, pair.Value);
                }
            }
            var json = JsonConvert.SerializeObject(dataList);

            var timeout = Task.Delay(3000);
            do
            {
                if (!Utilities.IsFileLocked(SongHashFile.FullName))
                {
                    File.WriteAllText(SongHashFile.FullName, json);
                    writeFinished = true;
                }
                if(!writeFinished)
                    await Task.Delay(100).ConfigureAwait(false);
            } while (!writeFinished || timeout.IsCompleted);
        }

        public DirectoryInfo SongsDirectory { get; set; }
        private bool IsDirty { get; set; }

        /// <summary>
        /// Parse the SongHashData file into the 'Data' Dictionary. If no file path is provided it uses the default path.
        /// </summary>
        /// <param name="filePath"></param>
        public void Initialize(string songsFolder, string filePath = "", string songCoreDataPath = "")
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
            }
            foreach (var item in Data.Keys)
            {
                Data[item].Directory = item;
            }
        }

        public void LoadSongCoreHashes(string songCoreHashPath = "")
        {

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
                if (folder.GetFiles().Any(f => f.Name.ToLower() == "info.dat"))
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
