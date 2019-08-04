using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static SongDownloadManager.Util.Util;
using SongDownloadManager.Util;

namespace SongDownloadManager
{
    public class DownloadJob
    {
        public const string NOTFOUNDERROR = "The remote server returned an error: (404) Not Found.";
        private const string TIMEOUTERROR = "The request was aborted: The request was canceled.";

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

        private DownloadResultBuilder resultBuilder = new DownloadResultBuilder();

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

        public JobResultCode ResultCode { get; protected set; }

        public async Task<bool> DownloadFile(string url, string path)
        {
            bool successful = true;
            ResultCode = JobResultCode.SUCCESS;

            FileInfo zipFile = new FileInfo(path);

            //var token = _tokenSource.Token;

            Task downloadAsync = WebUtils.DownloadFileAsync(url, path, true);
            try
            {
                await downloadAsync.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                
            }

            if (downloadAsync.IsFaulted || !File.Exists(path))
            {
                successful = false;
                if (downloadAsync.Exception != null)
                {
                    if (downloadAsync.Exception.InnerException.Message.Contains("404"))
                    {

                        resultBuilder.Reason = $"{url} was not found on Beat Saver.";
                        resultBuilder.Exception = downloadAsync.Exception.InnerException;
                        resultBuilder.Aborted = true;
                        ResultCode = JobResultCode.NOTFOUND;
                    }
                    else if (downloadAsync.Exception.InnerException.Message == TIMEOUTERROR)
                    {
                        resultBuilder.Reason = $"Download of {url} timed out.";
                        resultBuilder.Exception = downloadAsync.Exception.InnerException;
                        resultBuilder.Aborted = true;
                        ResultCode = JobResultCode.TIMEOUT;
                    }
                    else
                    {
                        resultBuilder.Reason = $"Error downloading {url}"; //, downloadAsync.Exception.InnerException);
                        resultBuilder.Exception = downloadAsync.Exception.InnerException;
                        resultBuilder.Aborted = true;
                        ResultCode = JobResultCode.OTHERERROR;
                    }
                }
                else
                {
                    ResultCode = JobResultCode.OTHERERROR;
                }
            }

            zipFile.Refresh();
            if (!(ResultCode == JobResultCode.SUCCESS) && zipFile.Exists)
            {
                //Logger.Warning($"Failed download, deleting {zipFile.FullName}");
                try
                {
                    var time = Stopwatch.StartNew();
                    bool waitTimeout = false;
                    while (!(IsFileLocked(zipFile.FullName) || !waitTimeout))
                        waitTimeout = time.ElapsedMilliseconds < 3000;
                    if (waitTimeout)
                        resultBuilder.AddWarning($"Timeout waiting for {zipFile.FullName} to be released for deletion.");
                    File.Delete(zipFile.FullName);
                }
                catch (System.IO.IOException)
                {
                    resultBuilder.AddWarning("File is in use and can't be deleted");
                }

                successful = false;
            }
            return successful;

        }



        public enum JobResultCode
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
