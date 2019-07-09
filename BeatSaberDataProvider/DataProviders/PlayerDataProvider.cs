using BeatSaberDataProvider.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeatSaberDataProvider.DataProviders
{
    public class PlayerDataProvider
    {
        public static readonly string DEFAULT_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low", @"Hyperbolic Magnetism\Beat Saber");
        public static readonly string BACKUP_FOLDER = Path.Combine(DEFAULT_FOLDER, "PlayerDataBackups");
        public const string DEFAULT_FILE_NAME = "PlayerData.dat";

        public static readonly Regex BackupFilePattern = new Regex(@"^(\d{8})-(\d{6}).(.+)", RegexOptions.Compiled);

        [JsonProperty("version")]
        public string version;
        [JsonProperty("localPlayers")]
        public List<PlayerData> localPlayers;
        [JsonProperty("guestPlayers")]
        public List<GuestPlayer> guestPlayers;
        [JsonProperty("lastSelectedBeatmapDifficulty")]
        public int lastSelectedBeatmapDifficulty;

        [JsonIgnore]
        public FileInfo CurrentFile;
        [JsonIgnore]
        public List<LevelStatsData> Data { get { return localPlayers?.FirstOrDefault()?.levelsStatsData; } }

        public PlayerDataProvider()
        {

        }

        public FileInfo[] GetBackupFiles(string directory = "")
        {
            if (string.IsNullOrEmpty(directory))
                directory = BACKUP_FOLDER;
            var dir = new DirectoryInfo(directory);
            var matchingFiles = dir.EnumerateFiles().Where(f => f.Name.ToLower().EndsWith(CurrentFile.Name.ToLower()));
            List<FileInfo> backups = new List<FileInfo>();
            foreach (var item in matchingFiles)
            {
                Match match = BackupFilePattern.Match(item.Name);
                if (match.Success)
                {
                    backups.Add(item);
                }
            }
            return backups.ToArray();
        }

        public void Initialize(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = Path.Combine(DEFAULT_FOLDER, DEFAULT_FILE_NAME);
            if (File.Exists(filePath))
            {
                var str = File.ReadAllText(filePath);
                JsonConvert.PopulateObject(str, this);
                CurrentFile = new FileInfo(filePath);
            }
        }

        public void WriteFile(string filePath = "")
        {
            FileInfo fileToWrite = CurrentFile;
            if (!string.IsNullOrEmpty(filePath))
                fileToWrite = new FileInfo(filePath);
            fileToWrite.Refresh();
            if (fileToWrite.Exists)
            {
                string copyPath = Path.Combine(BACKUP_FOLDER, $"{DateTime.Now.ToString("yyyyMMdd")}-{DateTime.Now.ToString("HHmmss")} {fileToWrite.Name}");
                if (!Directory.Exists(BACKUP_FOLDER))
                    Directory.CreateDirectory(BACKUP_FOLDER);
                fileToWrite.CopyTo(copyPath);
            }
            File.WriteAllText(fileToWrite.FullName, JsonConvert.SerializeObject(this));
        }
    }
}
