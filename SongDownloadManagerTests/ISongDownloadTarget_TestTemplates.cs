using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongDownloadManager;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var result = transferTask.Result;
            Assert.IsTrue(result);
        }

        public static void TransferSong_Cancelled(ISongDownloadTarget downloadTarget)
        {
            Directory.CreateDirectory(@"TestSourceSongs");
            Directory.CreateDirectory(@"TestSourceSongs\5381-4803 Moon Pluck");
            var sourceSong = @"TestSourceSongs\5381-4803 Moon Pluck";
            var cancelSource = new CancellationTokenSource();
            var transferTask = downloadTarget.TransferSong(sourceSong, true, cancelSource.Token);
            cancelSource.Cancel();
            try
            {
                var result = transferTask.Result;
            } catch(AggregateException ex)
            {
                Assert.IsTrue(ex.InnerExceptions.First().GetType() == typeof(TaskCanceledException));
            }
            

        }

        public static void TransferSongs_Test(ISongDownloadTarget downloadTarget)
        {
            var sourceSong = @"TestSourceSongs";
            var cancelSource = new CancellationTokenSource();
            var progressTest = new Action<string, int>((dir, progress) =>
            {
                Console.WriteLine($"{progress}% for {dir}");
                if (progress == 100)
                    cancelSource.Cancel();
            });
            var transferTask = downloadTarget.TransferSongs(sourceSong, true, progressTest, cancelSource.Token);
            var test = transferTask.Result;
            var hashes = downloadTarget.GetExistingSongHashesAsync().Result;
            
            Assert.IsTrue(hashes.Contains("63F2998EDBCE2D1AD31917E4F4D4F8D66348105D")); // Sun Pluck
            Assert.IsFalse(hashes.Contains("C8A4070B20E7B4DE3C4561297FF04C96CEBB991F")); // Moon Pluck
            //cancelSource.Cancel();
        }

        public static void IsValidTarget_IsValid_CreateIfMissing(ISongDownloadTarget downloadTarget)
        {
            var validTarget = downloadTarget.IsValidTarget();
            Assert.IsTrue(validTarget);
        }

        public static void IsValidTarget_IsValid_NoCreate(ISongDownloadTarget downloadTarget)
        {
            var validTarget = downloadTarget.IsValidTarget(false);
            Assert.IsTrue(validTarget);
        }

        public static void IsValidTarget_IsNotValid_CreateIfMissing(ISongDownloadTarget downloadTarget)
        {
            var validTarget = downloadTarget.IsValidTarget();
            Assert.IsFalse(validTarget);
        }

        public static void IsValidTarget_IsNotValid_NoCreate(ISongDownloadTarget downloadTarget)
        {
            var validTarget = downloadTarget.IsValidTarget(false);
            Assert.IsFalse(validTarget);
        }

        #region Invalid Input

        #endregion
    }
}
