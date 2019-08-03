using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongDownloadManager;
using System;
using System.IO;

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
        public void TransferSong_SourceOnly_Cancelled()
        {
            var target = new LocalDirectoryTarget("TestDestSongs");
            ISongDownloadTarget_TestTemplates.TransferSong_Cancelled(target);
        }

        [TestMethod]
        public void TransferSongs_SourceOnly()
        {
            var target = new LocalDirectoryTarget("TestDestSongs");
            ISongDownloadTarget_TestTemplates.TransferSongs_Test(target);
        }

        [TestMethod]
        public void IsValidTarget_IsValid_CreateIfMissing()
        {
            var testFolder = new DirectoryInfo("ValidCanCreateTest");
            if (testFolder.Exists)
                testFolder.Delete(true);
            testFolder.Refresh();

            var target = new LocalDirectoryTarget(testFolder.FullName);
            ISongDownloadTarget_TestTemplates.IsValidTarget_IsValid_CreateIfMissing(target);
        }

        [TestMethod]
        public void IsValidTarget_IsValid_NoCreate()
        {
            var testFolder = new DirectoryInfo("ValidNoCreateTest");
            testFolder.Create();

            var target = new LocalDirectoryTarget(testFolder.FullName);
            testFolder.Refresh();
            ISongDownloadTarget_TestTemplates.IsValidTarget_IsValid_NoCreate(target);
        }

        [TestMethod]
        public void IsValidTarget_IsNotValid_CreateIfMissing()
        {
            var invalidFolderName = new DirectoryInfo("Invalid*?");
            try
            {
                var target = new LocalDirectoryTarget(invalidFolderName.FullName, false);
                ISongDownloadTarget_TestTemplates.IsValidTarget_IsNotValid_CreateIfMissing(target);
            }catch(ArgumentException ex)
            {
                Assert.IsTrue(ex.ParamName.Equals("directory"));
            }
            
        }

        [TestMethod]
        public void IsValidTarget_IsNotValid_NoCreate()
        {
            var testFolder = new DirectoryInfo("InvalidNoCreateTest");
            var target = new LocalDirectoryTarget(testFolder.FullName);
            testFolder.Refresh();
            if (testFolder.Exists)
                testFolder.Delete(true);
            testFolder.Refresh();
            ISongDownloadTarget_TestTemplates.IsValidTarget_IsNotValid_NoCreate(target);
        }


        #region Invalid Input

        #endregion
    }
}
