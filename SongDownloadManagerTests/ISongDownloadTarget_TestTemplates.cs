using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongDownloadManager;
using System;
using System.IO;
using System.Threading;

namespace SongDownloadManagerTests
{
    public class ISongDownloadTarget_TestTemplates
    {
        public static bool destDeleted = false;
        static ISongDownloadTarget_TestTemplates()
        {
            var destFolder = new DirectoryInfo("TestDestSongs");
            if (destFolder.Exists)
            {
                destFolder.Delete(true);
                destFolder.Refresh();
                destDeleted = !destFolder.Exists;
            }
        }
        public static void TransferSongs(ISongDownloadTarget downloadTarget)
        {
            Directory.CreateDirectory(@"TestSourceSongs");
            Directory.CreateDirectory(@"TestSourceSongs\5381-4803 Moon Pluck");
            var sourceSong = @"TestSourceSongs\5381-4803 Moon Pluck";
            var cancelSource = new CancellationTokenSource();
            var transferTask = downloadTarget.TransferSong(sourceSong, true, cancelSource.Token);
            cancelSource.Cancel();

        }


        #region Invalid Input

        #endregion
    }
}
