using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SongDownloadManager
{
    public interface ISongDownloadManager
    {
        Dictionary<string, ISongDownloadTarget> DownloadTargets { get; }

        /// <summary>
        /// Attempts to add an ISongDownloadTarget to the DownloadTargets Dictionary.
        /// </summary>
        /// <param name="target"></param>
        void RegisterDownloadTarget(ISongDownloadTarget target);
        
        void DownloadSong(string songIdentifier, IEnumerable<ISongDownloadTarget> targets, Action<int> Progress, CancellationToken cancellationToken);
        void DownloadSong(string songIdentifier, IEnumerable<ISongDownloadTarget> targets, Action<int> Progress);
        void DownloadSong(string songIdentifier, IEnumerable<ISongDownloadTarget> targets, CancellationToken cancellationToken);
        void DownloadSong(string songIdentifier, IEnumerable<ISongDownloadTarget> targets);
       
    }
}
