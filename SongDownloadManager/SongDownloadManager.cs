using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SongDownloadManager
{
    public class SongDownloadManager : ISongDownloadManager
    {
        /// <summary>
        /// Hash needs to be lowercase.
        /// </summary>
        public const string BeatSaver_Hash_Download_Url_Base = "https://beatsaver.com/api/download/hash/";
        public const string BeatSaver_Key_Download_Url_Base = "https://beatsaver.com/api/download/key/";

        public virtual string DefaultSongDirectory { get; protected set; }
        public string TempDirectory { get; set; }
        private DirectoryInfo _defaultSongDirectory;

        /// <summary>
        /// Key: Hash, Value: downloaded zip
        /// </summary>
        public ConcurrentDictionary<string, string> DownloadedSongs { get; protected set; }

        public Dictionary<string, ISongDownloadTarget> DownloadTargets => new Dictionary<string, ISongDownloadTarget>();
        private object _downloadTargetsLock = new object();
        private ActionBlock<DownloadJob> DownloadQueue;
        private ActionBlock<DownloadJob> ExtractQueue;

        public SongDownloadManager()
        {
            DownloadQueue = new ActionBlock<DownloadJob>(async job =>
            {
                try
                {

                }
                catch (Exception ex)
                {

                }
            });


        }

        public Task<DownloadJob> QueueSongAsync(string songHash)
        {
            if (string.IsNullOrEmpty(songHash?.Trim()))
                throw new ArgumentNullException(nameof(songHash), "songHash cannot be null for SongDownloadManager.QueueSongAsync.");
            //if (downloadUri == null)
            //    throw new ArgumentNullException(nameof(downloadUri), "downloadUri cannot be null for SongDownloadManager.QueueSongAsync.");
            var downloadUri = new Uri(BeatSaver_Hash_Download_Url_Base + songHash.ToLower());
            var newJob = new DownloadJob(songHash, downloadUri, TempDirectory);

            return QueueJobAsync(newJob);
        }

        public async Task<DownloadJob> QueueJobAsync(DownloadJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job), "job cannot be null for SongDownloadManager.QueueJobAsync");
            //var songStatus = GetSongStatus(job.SongHash, Directory.GetParent(job.SongDirectory).FullName);
            //if (songStatus != SongDownloadStatus.NotFound && songStatus != SongDownloadStatus.Error)
            //{
            //    return job;
            //}
            await DownloadQueue.SendAsync(job).ConfigureAwait(false);
            return job;
        }

        public void RegisterDownloadTarget(ISongDownloadTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), "target cannot be null for RegisterDownloadTarget");
            if (string.IsNullOrEmpty(target.ID))
                throw new ArgumentException(nameof(target), "ISongDownloadTarget's ID cannot be null or empty.");
            lock (_downloadTargetsLock)
            {
                if (!DownloadTargets.ContainsKey(target.ID))
                {
                    DownloadTargets.Add(target.ID, target);
                }
            }
        }

        public void DeregisterDownloadTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
                throw new ArgumentNullException(nameof(targetId), "targetId cannot be null or empty for DeregisterDownloadTarget");
            lock (_downloadTargetsLock)
            {
                if (DownloadTargets.ContainsKey(targetId))
                    DownloadTargets.Remove(targetId);
            }
        }

        public void DeregisterDownloadTarget(ISongDownloadTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), "target cannot be null for DeregisterDownloadTarget");
            DeregisterDownloadTarget(target.ID);
        }


        public Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets, Action<int> Progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets, Action<int> Progress)
        {
            throw new NotImplementedException();
        }

        public Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets)
        {
            throw new NotImplementedException();
        }



    }
}
//public enum SongDownloadStatus
//{
//    NotFound,
//    Queued,
//    Downloading,
//    Downloaded,
//    Error
//}
//public SongDownloadStatus GetSongStatus(string songHash, string rootDirectory = "")
//{
//    var status = SongDownloadStatus.NotFound;
//    DirectoryInfo rootDir = string.IsNullOrEmpty(rootDirectory) ? _defaultSongDirectory : new DirectoryInfo(rootDirectory);
//    if (DownloadedSongs.TryGetValue(rootDir.FullName, out ConcurrentDictionary<string, SongDownloadStatus> songs))
//    {
//        if (!songs.TryGetValue(songHash.ToUpper(), out status))
//        {
//            status = SongDownloadStatus.NotFound;
//        }
//    }
//    return status;
//}
