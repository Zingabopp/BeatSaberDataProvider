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
        public static void TransferSong_Test(ISongDownloadTarget downloadTarget)
        {
            Directory.CreateDirectory(@"TestSourceSongs");
            Directory.CreateDirectory(@"TestSourceSongs\5381-4803 Moon Pluck");
            var sourceSong = @"TestSourceSongs\5381-4803 Moon Pluck";
            var cancelSource = new CancellationTokenSource();
            var transferTask = downloadTarget.TransferSong(sourceSong, true, cancelSource.Token);
            cancelSource.Cancel();
        }

        public static void TransferSongs_Test(ISongDownloadTarget downloadTarget)
        {
            var sourceSong = @"TestSourceSongs";
            var cancelSource = new CancellationTokenSource();
            var progressTest = new Action<string, int>((dir, progress) =>
            {
                Console.WriteLine($"{progress}% for {dir}");
            });
            var transferTask = downloadTarget.TransferSongs(sourceSong, true, progressTest, cancelSource.Token);
            var test = transferTask.Result;
            var hashes = downloadTarget.GetExistingSongHashesAsync().Result;
            Assert.IsTrue(hashes.Contains("C8A4070B20E7B4DE3C4561297FF04C96CEBB991F")); // Moon Pluck
            Assert.IsTrue(hashes.Contains("63F2998EDBCE2D1AD31917E4F4D4F8D66348105D")); // Sun Pluck
            //cancelSource.Cancel();
        }


        #region Invalid Input

        #endregion
    }
}
