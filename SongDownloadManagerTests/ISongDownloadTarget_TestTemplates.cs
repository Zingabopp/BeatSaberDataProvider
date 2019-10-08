using BeatSaberDataProvider.DataProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongDownloadManager;
using System;
using System.Collections.Generic;
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
            var sourceSong = new SongDownload() { Hash = "asdf", LocalDirectory = new DirectoryInfo(@"TestSourceSongs\5381-4803 Moon Pluck") };
            var cancelSource = new CancellationTokenSource();
            var transferTask = downloadTarget.TransferSong(sourceSong, true, cancelSource.Token);
            var result = transferTask.Result;
            Assert.IsTrue(result);
        }

        public static void TransferSong_Cancelled(ISongDownloadTarget downloadTarget)
        {
            Directory.CreateDirectory(@"TestSourceSongs");
            Directory.CreateDirectory(@"TestSourceSongs\5381-4803 Moon Pluck");
            var sourceSong = new SongDownload() { Hash = "asdf", LocalDirectory = new DirectoryInfo(@"TestSourceSongs\5381-4803 Moon Pluck") };
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
            //var sourceSong = @"TestSourceSongs";
            var sourceDir = new DirectoryInfo(@"TestSourceSongs");
            var sourceHashes = new List<string>();
            foreach (var songDir in sourceDir.GetDirectories())
            {
                if(songDir.EnumerateFiles("info.dat").Any())
                    sourceHashes.Add(SongHashDataProvider.GenerateHash(songDir.FullName));
            }
            
            var cancelSource = new CancellationTokenSource();
            var progressTest = new Action<string, int>((dir, progress) =>
            {
                Console.WriteLine($"{progress}% for {dir}");
                //if (progress == 100)
                    //cancelSource.Cancel();
            });
            var sourceSong = new SongDownload() { Hash = "asdf", LocalDirectory = new DirectoryInfo(@"TestSourceSongs\5381-4803 Moon Pluck") };
            var songs = new List<SongDownload>() { sourceSong };
            var transferTask = downloadTarget.TransferSongs(songs, true, progressTest, cancelSource.Token);
            var test = transferTask.Result;
            var hashes = downloadTarget.GetExistingSongHashesAsync().Result;
            foreach (var sourceHash in sourceHashes)
            {
                Assert.IsTrue(hashes.Contains(sourceHash));
            }
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
