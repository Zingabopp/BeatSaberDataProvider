using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SongDownloadManager
{
    public class DownloadJob
    {
        //public const string BEATSAVER_DOWNLOAD_URL_BASE = "https://beatsaver.com/api/download/key/";
        /// <summary>
        /// Hash needs to be lowercase.
        /// </summary>
        public const string BeatSaver_Download_Url_Base = "https://beatsaver.com/api/download/hash/";





        public DownloadJob(string songHash, string songDirectory, string tempDirectory)
        {
            
            if (string.IsNullOrEmpty(songHash?.Trim()))
                throw new ArgumentNullException(nameof(songHash), "songHash cannot be null for SongDownloadManager.QueueSongAsync.");
            if (string.IsNullOrEmpty(songDirectory?.Trim()))
                throw new ArgumentNullException(nameof(songDirectory), "songDirectory cannot be null for SongDownloadManager.QueueSongAsync.");
            if (string.IsNullOrEmpty(tempDirectory?.Trim()))
                throw new ArgumentNullException(nameof(tempDirectory), "tempDirectory cannot be null for SongDownloadManager.QueueSongAsync.");
            DownloadUri = new Uri(BeatSaver_Download_Url_Base + songHash.ToLower());
            var thing = new DirectoryInfo(songDirectory);
            
            var test = Path.GetFullPath(songDirectory);
        }

        public Uri DownloadUri { get; protected set; }

        public string SongHash { get; protected set; }

        /// <summary>
        /// Folder the song files go in
        /// </summary>
        public string SongDirectory { get; protected set; }

        /// <summary>
        /// Temp root folder the zip is extracted to
        /// </summary>
        public string TempDirectory { get; protected set; }

        /// <summary>
        /// Path to the zip file
        /// </summary>
        public string ZipPath { get; protected set; }

        public JobResult Result { get; protected set; }

        public enum JobResult
        {
            NOTSTARTED,
            SUCCESS,
            TIMEOUT,
            NOTFOUND,
            UNZIPFAILED,
            OTHERERROR,
            Exists
        }

        public enum JobStatus
        {
            NotStarted,
            Running,
            Completed,
            Error
        }
    }
}
