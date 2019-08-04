using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SongDownloadManager
{
    public class DownloadJob
    {
        public DownloadJob(string songHash, Uri downloadUri, string tempDirectory)
        {
            
            if (string.IsNullOrEmpty(songHash?.Trim()))
                throw new ArgumentNullException(nameof(songHash), "songHash cannot be null for SongDownloadManager.QueueSongAsync.");
            if (string.IsNullOrEmpty(tempDirectory?.Trim()))
                throw new ArgumentNullException(nameof(tempDirectory), "tempDirectory cannot be null for SongDownloadManager.QueueSongAsync.");
            DownloadUri = downloadUri ?? throw new ArgumentNullException(nameof(downloadUri), "downloadUri cannot be null for SongDownloadManager.QueueSongAsync.");
            SongHash = songHash;
            TempDirectory = tempDirectory;
            SongDirectory = Path.Combine(TempDirectory, SongHash);
        }

        public Uri DownloadUri { get; protected set; }

        public string SongHash { get; protected set; }

        /// <summary>
        /// Temp root folder the zip is extracted to
        /// </summary>
        public string TempDirectory { get; protected set; }

        /// <summary>
        /// Temp folder the song files are extracted to.
        /// </summary>
        public string SongDirectory { get; protected set; }

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
