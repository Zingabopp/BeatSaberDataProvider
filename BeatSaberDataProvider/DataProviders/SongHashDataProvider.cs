using BeatSaberDataProvider.Util;
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
using System.Security.Cryptography;

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

        #region Statics
        /// <summary>
        /// Generates a hash for the song and assigns it to the SongHash field.
        /// Uses Kylemc1413's implementation from SongCore.
        /// TODO: Handle/document exceptions (such as if the files no longer exist when this is called).
        /// https://github.com/Kylemc1413/SongCore
        /// </summary>
        /// <returns>Hash of the song files. Null if the info.dat file doesn't exist</returns>
        public static string GenerateHash(string songDirectory, string existingHash = "")
        {
            byte[] combinedBytes = Array.Empty<byte>();
            string infoFile = Path.Combine(songDirectory, "info.dat");
            if (!File.Exists(infoFile))
                return null;
            combinedBytes = combinedBytes.Concat(File.ReadAllBytes(infoFile)).ToArray();
            var token = JToken.Parse(File.ReadAllText(infoFile));
            var beatMapSets = token["_difficultyBeatmapSets"];
            int numChars = beatMapSets.Children().Count();
            for (int i = 0; i < numChars; i++)
            {
                var diffs = beatMapSets.ElementAt(i);
                int numDiffs = diffs["_difficultyBeatmaps"].Children().Count();
                for (int i2 = 0; i2 < numDiffs; i2++)
                {
                    var diff = diffs["_difficultyBeatmaps"].ElementAt(i2);
                    string beatmapPath = Path.Combine(songDirectory, diff["_beatmapFilename"].Value<string>());
                    if (File.Exists(beatmapPath))
                        combinedBytes = combinedBytes.Concat(File.ReadAllBytes(beatmapPath)).ToArray();
                }
            }

            string hash = CreateSha1FromBytes(combinedBytes.ToArray());
            if (!string.IsNullOrEmpty(existingHash) && existingHash != hash)
                Logger.Warning($"Hash doesn't match the existing hash for {songDirectory}");
            return hash;
        }

        /// <summary>
        /// Returns the Sha1 hash of the provided byte array.
        /// Uses Kylemc1413's implementation from SongCore.
        /// https://github.com/Kylemc1413/SongCore
        /// </summary>
        /// <param name="input">Byte array to hash.</param>
        /// <returns>Sha1 hash of the byte array.</returns>
        public static string CreateSha1FromBytes(byte[] input)
        {
            using (var sha1 = SHA1.Create())
            {
                var inputBytes = input;
                var hashBytes = sha1.ComputeHash(inputBytes);

                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }

        /// <summary>
        /// Generates a quick hash of a directory's contents. Does NOT match SongCore.
        /// Uses most of Kylemc1413's implementation from SongCore.
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when path's directory doesn't exist.</exception>
        /// <returns></returns>
        public static long GenerateDirectoryHash(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "Path cannot be null or empty for GenerateDirectoryHash");

            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException($"GenerateDirectoryHash couldn't find {path}");
            long dirHash = 0L;
            foreach (var file in directoryInfo.GetFiles())
            {
                dirHash ^= file.CreationTimeUtc.ToFileTimeUtc();
                dirHash ^= file.LastWriteTimeUtc.ToFileTimeUtc();
                dirHash ^= SumCharacters(file.Name);
                dirHash ^= file.Length;
            }
            return dirHash;
        }

        private static int SumCharacters(string str)
        {
            unchecked
            {
                int charSum = 0;
                for (int i = 0; i < str.Count(); i++)
                {
                    charSum += str[i];
                }
                return charSum;
            }
        }

        #endregion


    }


}
