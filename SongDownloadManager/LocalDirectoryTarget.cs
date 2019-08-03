﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberDataProvider.DataModels;

namespace SongDownloadManager
{
    public class LocalDirectoryTarget : ISongDownloadTarget
    {
        public DirectoryInfo TargetDirectory { get; private set; }
        public ConcurrentDictionary<string, string> Songs { get; private set; }
        private Dictionary<string, SongHashData> ExistingHashes { get; set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="targetDescription"></param>
        /// <param name="directory"></param>
        /// <param name="createIfMissing"></param>
        /// <exception cref="ArgumentNullException">Thrown when directory is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the target directory does not exist and createIfMissing is false.</exception>
        /// <exception cref="IOException">Thrown when the target directory cannot be created.</exception>
        public LocalDirectoryTarget(string targetName, string targetDescription, string directory, bool createIfMissing = true)
        {
            if (string.IsNullOrEmpty(directory?.Trim()))
                throw new ArgumentNullException(nameof(directory), "directory cannot be null when creating a SongDirectory.");
            ExistingHashes = new Dictionary<string, SongHashData>();
            Songs = new ConcurrentDictionary<string, string>();
            TargetDirectory = new DirectoryInfo(directory);
            if (!TargetDirectory.Exists)
            {
                if (!createIfMissing)
                    throw new ArgumentException(string.Format("Target directory, {0}, does not exist and createIfMissing is false.", directory), nameof(directory));
                TargetDirectory.Create();
            }
            Name = string.IsNullOrEmpty(targetName) ? TargetDirectory.Name : targetName;
            Description = string.IsNullOrEmpty(targetName) ? TargetDirectory.FullName : targetDescription;
        }

        public LocalDirectoryTarget(string directory, bool createIfMissing = true)
            : this(null, null, directory, createIfMissing)
        { }

        public async Task<List<string>> GetExistingSongHashesAsync(bool hashExisting)
        {
            foreach (var songDir in TargetDirectory.GetDirectories())
            {
                if (!ExistingHashes.ContainsKey(songDir.FullName))
                {
                    ExistingHashes.Add(songDir.FullName, new SongHashData(songDir.FullName));
                }
            }
            List<SongHashData> songsToHash;
            if (hashExisting)
                songsToHash = ExistingHashes.Values.ToList();
            else
                songsToHash = ExistingHashes.Values.Where(hashData => string.IsNullOrEmpty(hashData.SongHash)).ToList();
            await Task.Run(() => songsToHash.AsParallel().ForAll(h =>
            {
                h.GenerateDirectoryHash();
                h.GenerateHash();
            })).ConfigureAwait(false);
            return ExistingHashes.Values.Select(h => h.SongHash).ToList();
        }

        public Task<List<string>> GetExistingSongHashesAsync()
        {
            return GetExistingSongHashesAsync(false);
        }

        public async Task<bool> TransferSong(string source, bool overwrite, Action<int> ProgressPercent, CancellationToken cancellationToken)
        {
            var createdFiles = new List<FileInfo>();
            bool targetDirCreated = false;
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException(nameof(source), "source directory cannot be null for LocalDirectoryTarget.TransferSong");
            var sourceDir = new DirectoryInfo(source);
            var targetDir = new DirectoryInfo(Path.Combine(TargetDirectory.FullName, sourceDir.Name));
            if (!targetDir.Exists)
            {
                targetDir.Create();
                targetDirCreated = true;
            }
            if (!sourceDir.Exists)
                throw new ArgumentException(string.Format("source directory does not exist: {0}", source), nameof(source));
            bool success = true;
            foreach (string sourceFile in sourceDir.EnumerateFiles().Select(f => f.FullName))
            {

                if (!success || cancellationToken.IsCancellationRequested)
                {
                    success = false;
                    break;
                }
                var destFile = new FileInfo(Path.Combine(targetDir.FullName, sourceFile.Substring(sourceFile.LastIndexOf('\\') + 1)));
                using (FileStream SourceStream = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {

                    using (FileStream DestinationStream = File.Create(destFile.FullName))
                    {
                        await SourceStream.CopyToAsync(DestinationStream).ConfigureAwait(false);
                    }
                }
                destFile.Refresh();
                if (!destFile.Exists)
                    success = false;
                else
                {
                    createdFiles.Add(destFile);
                    destFile.CreationTimeUtc = File.GetCreationTimeUtc(sourceFile);
                    destFile.LastWriteTimeUtc = File.GetLastWriteTimeUtc(sourceFile);
                }
            }
            if (success)
            {
                // Check files are valid?

            }
            else // Copy failed, clean up
            {
                if (targetDirCreated)
                {
                    targetDir.Delete(true);
                }
                else
                {
                    foreach (var file in createdFiles)
                    {
                        file.Delete();
                    }
                }

            }

            return success;
        }

        public async Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, bool overwrite, Action<string, int> SongProgressPercent, CancellationToken cancellationToken)
        {
            var retDict = new Dictionary<string, bool>();
            var dir = new DirectoryInfo(sourceDirectory);
            foreach (var songDir in dir.EnumerateDirectories(sourceDirectory))
            {
                if (!songDir.EnumerateFiles("info.dat", SearchOption.TopDirectoryOnly).Any())
                    continue;
                Action<int> songProgress = null;
                if(SongProgressPercent != null)
                {
                    songProgress = 
                }
                retDict.Add(songDir.FullName, await TransferSong(songDir.FullName, overwrite, SongProgressPercent, cancellationToken));
            }
        }

        #region TransferSong Overloads
        public Task<bool> TransferSong(string source, Action<int> SongProgressPercent, CancellationToken cancellationToken)
        {
            return TransferSong(source, false, SongProgressPercent, cancellationToken);
        }

        public Task<bool> TransferSong(string source, bool overwrite, CancellationToken cancellationToken)
        {
            return TransferSong(source, overwrite, null, cancellationToken);
        }

        public Task<bool> TransferSong(string source, bool overwrite)
        {
            return TransferSong(source, overwrite, null, CancellationToken.None);
        }

        public async Task<bool> TransferSong(string source)
        {
            var test = await TransferSong(source, false, null, CancellationToken.None).ConfigureAwait(false);
            return test;
        }
        #endregion

        #region TransferSongs Overloads
        public Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, Action<string, int> SongProgressPercent, CancellationToken cancellationToken)
        {
            return TransferSongs(sourceDirectory, false, SongProgressPercent, cancellationToken);
        }

        public Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, bool overwrite, CancellationToken cancellationToken)
        {
            return TransferSongs(sourceDirectory, overwrite, null, cancellationToken);
        }

        public Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, bool overwrite)
        {
            return TransferSongs(sourceDirectory, overwrite, null, CancellationToken.None);
        }

        public Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory)
        {
            return TransferSongs(sourceDirectory, false, null, CancellationToken.None);
        }
        #endregion
    }
}
