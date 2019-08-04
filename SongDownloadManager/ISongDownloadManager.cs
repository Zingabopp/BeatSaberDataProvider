using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongDownloadManager
{
    public interface ISongDownloadManager
    {
        /// <summary>
        /// Dictionary of available ISongDownloadTargets. Key is ISongDownloadTarget.ID.
        /// </summary>
        Dictionary<string, ISongDownloadTarget> DownloadTargets { get; }

        string TempDirectory { get; set; }

        /// <summary>
        /// Attempts to add an ISongDownloadTarget to the DownloadTargets Dictionary.
        /// </summary>
        /// <param name="target"></param>
        void RegisterDownloadTarget(ISongDownloadTarget target);

        void DeregisterDownloadTarget(ISongDownloadTarget target);
        void DeregisterDownloadTarget(string targetId);

        Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets, Action<int> Progress, CancellationToken cancellationToken);
        Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets, Action<int> Progress);
        Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets, CancellationToken cancellationToken);
        Task<DownloadResult> DownloadSongAsync(string songIdentifier, ICollection<ISongDownloadTarget> targets);

    }

    
}
