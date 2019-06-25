using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BeatSaberDataProvider.DataModels;

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
                JsonConvert.PopulateObject(str, Data);
                CurrentFile = new FileInfo(filePath);
            }
        }

    }

    
}
