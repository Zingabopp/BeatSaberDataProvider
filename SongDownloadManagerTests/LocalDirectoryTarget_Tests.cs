using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongDownloadManager;
using System;

namespace SongDownloadManagerTests
{
    [TestClass]
    public class LocalDirectoryTarget_Tests
    {
        [TestMethod]
        public void TransferSong_SourceOnly()
        {
            var target = new LocalDirectoryTarget("TestDestSongs");
            ISongDownloadTarget_TestTemplates.TransferSong_Test(target);
        }

        [TestMethod]
        public void TransferSongs_SourceOnly()
        {
            var target = new LocalDirectoryTarget("TestDestSongs");
            ISongDownloadTarget_TestTemplates.TransferSongs_Test(target);
        }


        #region Invalid Input

        #endregion
    }
}
