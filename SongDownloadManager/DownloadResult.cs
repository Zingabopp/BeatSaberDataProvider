using System;
using System.Collections.Generic;
using System.Text;

namespace SongDownloadManager
{
    public class DownloadResultBuilder
    {
        public bool Successful { get; set; }
        public string Reason { get; set; }
        public List<string> Warnings { get; }
        public Exception Exception { get; set; }
        public bool Aborted { get; set; }

        public DownloadResultBuilder()
        {
            Reason = string.Empty;
            Warnings = new List<string>();
            Exception = null;
            Aborted = false;
        }

        public void AddWarning(string warning)
        {
            if(!string.IsNullOrEmpty(warning))
                Warnings.Add(warning);
        }

        public DownloadResult ToDownloadResult()
        {
            return new DownloadResult(Successful, Reason, Warnings.ToArray(), Exception);
        }
    }

    public class DownloadResult
    {
        /// <summary>
        /// Whether or not the job was successful.
        /// </summary>
        public bool Successful { get; protected set; }
        /// <summary>
        /// Reason string for the critical error that aborted the job.
        /// </summary>
        public string Reason { get; protected set; }
        /// <summary>
        /// Non-critical warnings/errors that occurred, but didn't stop job execution.
        /// </summary>
        public IEnumerable<string> Warnings { get; protected set; }
        /// <summary>
        /// First critical exception that caused the job to abort.
        /// </summary>
        public Exception Exception { get; protected set; }

        public DownloadResult(bool successful, string reason = "", string[] warnings = null, Exception ex = null)
        {
            Successful = successful;
            Reason = reason;
            Exception = ex;
            Warnings = warnings;
        }
    }

    
}
