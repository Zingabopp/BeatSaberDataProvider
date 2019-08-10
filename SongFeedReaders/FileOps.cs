﻿using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders
{
    public static class FileOps
    {
        private static FeedReaderLoggerBase _logger = new FeedReaderLogger(LoggingController.DefaultLogController);
        public static FeedReaderLoggerBase Logger { get { return _logger; } set { _logger = value; } }

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
        public static async Task<string[]> ExtractZipAsync(string zipPath, string extractDirectory, bool deleteZip = true, bool overwriteTarget = true)
        {
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentNullException(nameof(zipPath));
            if (string.IsNullOrEmpty(extractDirectory))
                throw new ArgumentNullException(nameof(extractDirectory));

            FileInfo zipFile = new FileInfo(zipPath);
            DirectoryInfo extDir = new DirectoryInfo(extractDirectory);
            if (!zipFile.Exists)
                throw new ArgumentException($"File at zipPath {zipFile.FullName} does not exist.", nameof(zipPath));
            extDir.Create();
            //var extractedFiles = await ExtractAsync(zipFile.FullName, extDir.FullName, overwriteTarget).ConfigureAwait(true);
            string[] extractedFiles = null;
            if (await Utilities.WaitUntil(() =>
            {
                try
                {
                    using (ZipArchive zipArchive = ZipFile.OpenRead(zipPath))
                        zipArchive.ExtractToDirectory(extDir.FullName);
                    return true;
#pragma warning disable CA1031 // Do not catch general exception types
                }
                catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    return false;
                }

            }, 25, 3000).ConfigureAwait(false))
            {

            }
            if (deleteZip)
            {
                try
                {
                    var deleteSuccessful = await TryDeleteAsync(zipFile.FullName).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    Logger.Warning($"Unable to delete file {zipFile.FullName}.\n{ex.Message}\n{ex.StackTrace}");
                }
            }
            return extractedFiles;
        }

        public static string GetSafeDirectoryPath(string directory)
        {
            StringBuilder retStr = new StringBuilder(directory);
            foreach (var character in Path.GetInvalidPathChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }

        public static string GetSafeFileName(string fileName)
        {
            StringBuilder retStr = new StringBuilder(fileName);
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }

        private static Task<bool> TryDeleteAsync(string filePath)
        {
            var timeoutSource = new CancellationTokenSource(3000);
            var timeoutToken = timeoutSource.Token;
            return Utilities.WaitUntil(() =>
            {
                try
                {
                    File.Delete(filePath);
                    timeoutSource.Dispose();
                    return true;
                }
                catch (Exception)
                {
                    timeoutSource.Dispose();
                    throw;
                }
            }, timeoutToken);
        }

        private enum ZipExtractResult
        {

        }
    }
}