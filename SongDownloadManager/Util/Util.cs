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
    }
}
