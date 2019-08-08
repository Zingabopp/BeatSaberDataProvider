using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders
{
    public static class FileOps
    {
        /// <summary>
        /// Downloads a file from the specified URI to the specified path (path include file name).
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<string> DownloadFileAsync(Uri uri, string path, bool overwrite = true)
        {
            string actualPath = path;
            using (var response = await WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    return null;
                actualPath = await response.Content.ReadAsFileAsync(path, overwrite).ConfigureAwait(false);
            }
            return actualPath;
        }

        /// <summary>
        /// Extracts a zip file to the specified directory.
        /// </summary>
        /// <param name="zipPath">Path to zip file</param>
        /// <param name="extractDirectory">Directory to extract to</param>
        /// <param name="deleteZip">If true, deletes zip file after extraction</param>
        /// <param name="overwriteTarget">If true, overwrites existing files with the zip's contents</param>
        /// <returns></returns>
        public static async Task<string[]> ExtractZip(string zipPath, string extractDirectory, bool deleteZip = true, bool overwriteTarget = true)
        {
            throw new NotImplementedException();
        }

        public static string GetSafeDirectoryPath(string directory)
        {
            throw new NotImplementedException();
        }

        public static string GetSafeFileName(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
