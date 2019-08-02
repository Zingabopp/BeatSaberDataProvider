using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeatSaberDataProvider.DataModels;

namespace SongDownloadManager
{
    public class SongsDirectory
    {
        public DirectoryInfo Directory { get; private set; }
        public ConcurrentDictionary<string, string> Songs { get; private set; }
        private Dictionary<DirectoryInfo, SongHashData> ExistingHashes { get; set; }

        public SongsDirectory(DirectoryInfo directory, bool createIfMissing = true)
        {
            Songs = new ConcurrentDictionary<string, string>();
            Directory = directory ?? throw new ArgumentNullException(nameof(directory), "directory cannot be null when creating a SongDirectory.");
            if (!Directory.Exists)
            {
                if (!createIfMissing)
                    throw new ArgumentException("Directory doesn't exist and createIfMissing is false.", nameof(directory));
                Directory.Create();
            }
            else
            {
                foreach (var songDir in Directory.GetDirectories())
                {

                }
            }

            
        }

        public SongsDirectory(string directoryPath, bool createIfMissing = true)
            : this(new DirectoryInfo(directoryPath), createIfMissing)
        { }
    }
}
