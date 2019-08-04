using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SongDownloadManager
{
    public class SongDownloadManager : ISongDownloadManager
    {
        public virtual string DefaultSongDirectory { get; protected set; }
        public string TempDirectory { get; set; }
        private DirectoryInfo _defaultSongDirectory;

        /// <summary>
        /// First key is the root song directory to support multiple download locations, 2nd level key is song hash, value is the SongDownloadStatus.
        /// </summary>
        public ConcurrentDictionary<string, ConcurrentDictionary<string, SongDownloadStatus>> DownloadedSongs { get; protected set; }
        private ActionBlock<DownloadJob> DownloadQueue;

        public SongDownloadStatus GetSongStatus(string songHash, string rootDirectory = "")
        {
            var status = SongDownloadStatus.NotFound;
            DirectoryInfo rootDir = string.IsNullOrEmpty(rootDirectory) ? _defaultSongDirectory : new DirectoryInfo(rootDirectory);
            if (DownloadedSongs.TryGetValue(rootDir.FullName, out ConcurrentDictionary<string, SongDownloadStatus> songs))
            {
                if (!songs.TryGetValue(songHash.ToUpper(), out status))
                {
                    status = SongDownloadStatus.NotFound;
                }
            }
            return status;
        }


        public async Task<DownloadJob> QueueSongAsync(string songHash, Uri downloadUri, string songFolderName, string rootDirectory = "")
        {
            if (string.IsNullOrEmpty(songHash?.Trim()))
                throw new ArgumentNullException(nameof(songHash), "songHash cannot be null for SongDownloadManager.QueueSongAsync.");
            if (string.IsNullOrEmpty(songFolderName?.Trim()))
                throw new ArgumentNullException(nameof(songHash), "songFolderName cannot be null for SongDownloadManager.QueueSongAsync.");
            if (downloadUri == null)
                throw new ArgumentNullException(nameof(downloadUri), "downloadUri cannot be null for SongDownloadManager.QueueSongAsync.");
            var songStatus = GetSongStatus(songHash, rootDirectory);
            var newJob = new DownloadJob(songHash, songFolderName, TempDirectory);
            if (songStatus != SongDownloadStatus.NotFound && songStatus != SongDownloadStatus.Error)
            {
                
                return newJob;
            }
            return await QueueJobAsync(newJob).ConfigureAwait(false);
        }

        public async Task<DownloadJob> QueueJobAsync(DownloadJob job)
        {
            return job;
        }

        public enum SongDownloadStatus
        {
            NotFound,
            Queued,
            Downloading,
            Downloaded,
            Error
        }

    }
}
