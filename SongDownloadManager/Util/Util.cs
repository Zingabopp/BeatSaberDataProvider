using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SongDownloadManager.Util
{
    public static class Util
    {
        public static string MakeSafeFilename(string str)
        {
            StringBuilder retStr = new StringBuilder(str);
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }

        public static string MakeSafeDirectoryName(string str)
        {
            StringBuilder retStr = new StringBuilder(str);
            foreach (var character in Path.GetInvalidPathChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }

        public static bool IsFileLocked(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return !(inputStream.Length > 0);
            }
            catch (Exception) // TODO: Catch only IOException? so the caller doesn't wait for a Timeout if there are other errors
            {
                return true;
            }
        }
    }
}
