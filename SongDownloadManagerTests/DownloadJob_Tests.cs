using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongDownloadManager;
using System;

namespace SongDownloadManagerTests
{
    [TestClass]
    public class DownloadJob_Constructor_Tests
    {
        [TestMethod]
        public void Valid_Input()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            string songDirectory = "SongDir";
            string tempDirectory = "TempDir";
            var validJob = new DownloadJob(songHash, songDirectory, tempDirectory);
        }

        [TestMethod]
        public void Valid_Uri()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            Uri expectedUri = new Uri("https://beatsaver.com/api/download/hash/2c908df9bb7aa93884ab9bfa8ddc598c3de479e9");
            string songDirectory = "SongDir?!@*)$&$#^_)(*";
            string tempDirectory = "TempDir";
            var validJob = new DownloadJob(songHash, songDirectory, tempDirectory);
            Assert.AreEqual(expectedUri, validJob.DownloadUri);
        }


        #region Invalid Input
        [TestMethod]
        public void Invalid_SongHash_Null()
        {
            string songHash = null;
            string songDirectory = "SongDir";
            string tempDirectory = "TempDir";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("songHash", expectedException?.ParamName);
            } catch(Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_SongHash_Empty()
        {
            string songHash = "";
            string songDirectory = "SongDir";
            string tempDirectory = "TempDir";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("songHash", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_SongHash_WhiteSpace()
        {
            string songHash = "     ";
            string songDirectory = "SongDir";
            string tempDirectory = "TempDir";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("songHash", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_SongDir_Null()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            string songDirectory = null;
            string tempDirectory = "TempDir";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("songDirectory", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_SongDir_Empty()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            string songDirectory = "";
            string tempDirectory = "TempDir";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("songDirectory", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_SongDir_WhiteSpace()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            string songDirectory = "    ";
            string tempDirectory = "TempDir";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("songDirectory", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_TempDirectory_Null()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            string songDirectory = "SongDirectory";
            string tempDirectory = null;
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("tempDirectory", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_TempDirectory_Empty()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            string songDirectory = "SongDirectory";
            string tempDirectory = "";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("tempDirectory", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }

        [TestMethod]
        public void Invalid_TempDirectory_WhiteSpace()
        {
            string songHash = "2C908DF9BB7AA93884AB9BFA8DDC598C3DE479E9";
            string songDirectory = "SongDirectory";
            string tempDirectory = "    ";
            try
            {
                var invalidJob = new DownloadJob(songHash, songDirectory, tempDirectory);
                Assert.Fail("An ArgumentNullException should have been thrown.");
            }
            catch (AssertFailedException) { throw; }
            catch (ArgumentNullException expectedException)
            {
                Assert.AreEqual("tempDirectory", expectedException?.ParamName);
            }
            catch (Exception unexpectedException)
            {
                Assert.Fail($"The wrong exception was thrown. Expected ArgumentNullException, caught {unexpectedException.GetType().ToString()}");
            }
        }
        #endregion
    }
}
