using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongDownloadManager
{
    public interface ISongDownloadManager
    {
        Dictionary<string, ISongDownloadTarget> DownloadTargets { get; }

        string TempDirectory { get; set; }

        /// <summary>
        /// Attempts to add an ISongDownloadTarget to the DownloadTargets Dictionary.
        /// </summary>
        /// <param name="target"></param>
        void RegisterDownloadTarget(ISongDownloadTarget target);

        Task<DownloadResult> DownloadSongAsync(string songIdentifier, IEnumerable<ISongDownloadTarget> targets, Action<int> Progress, CancellationToken cancellationToken);
        Task<DownloadResult> DownloadSongAsync(string songIdentifier, IEnumerable<ISongDownloadTarget> targets, Action<int> Progress);
        Task<DownloadResult> DownloadSongAsync(string songIdentifier, IEnumerable<ISongDownloadTarget> targets, CancellationToken cancellationToken);
        Task<DownloadResult> DownloadSongAsync(string songIdentifier, IEnumerable<ISongDownloadTarget> targets);

    }

    public class DownloadResult
    {
        public bool Successful { get; private set; }
        public string Reason { get; private set; }
        public Exception Exception { get; private set; }

        public DownloadResult(bool successful, string reason = "", Exception ex = null)
        {
            Successful = successful;
            Reason = reason;
            Exception = ex;
        }
    }
}
