using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SongDownloadManager.Util.Util;

namespace SongDownloadManager
{
    public class LocalDirectoryTarget : ISongDownloadTarget
    {
        private object _existingHashesLock = new object();
        private const string IdPrefix = "localdirectory:";
        public DirectoryInfo TargetDirectory { get; private set; }
        public ConcurrentDictionary<string, string> Songs { get; private set; }
        private Dictionary<string, string> ExistingHashes { get; set; }

        public string ID { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public SongFormat ReceivingFormat => SongFormat.Extracted;

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
            ExistingHashes = new Dictionary<string, string>();
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
            ID = IdPrefix + TargetDirectory.FullName;
        }

        public LocalDirectoryTarget(string directory, bool createIfMissing = true)
            : this(null, null, directory, createIfMissing)
        { }

        public async Task<List<string>> GetExistingSongHashesAsync(bool hashExisting)
        {
            foreach (var songDir in TargetDirectory.GetDirectories())
            {
                // Add missing directories to the Dictionary with an empty string for the hash value.
                if (!ExistingHashes.ContainsKey(songDir.FullName))
                {
                    ExistingHashes.Add(songDir.FullName, string.Empty);
                }
            }
            List<string> songsToHash;
            if (hashExisting)
            {
                songsToHash = ExistingHashes.Keys.ToList();
            }
            else
                songsToHash = ExistingHashes.Values.Where(hashData => string.IsNullOrEmpty(hashData)).ToList();


            await Task.Run(() => songsToHash.AsParallel().ForAll(h =>
            {
                lock (_existingHashesLock)
                {
                    if (ExistingHashes.ContainsKey(h))
                        ExistingHashes[h] = GenerateHash(h);
                    else
                        ExistingHashes.Add(h, GenerateHash(h));
                }
            })).ConfigureAwait(false);

            return ExistingHashes.Values.ToList();
        }

        public Task<List<string>> GetExistingSongHashesAsync()
        {
            return GetExistingSongHashesAsync(false);
        }

        public void LoadExistingSongHashes(Dictionary<string, string> hashes)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes), "hashes Dictionary cannot be null for LoadExistingSongHashes");
            foreach (var pair in hashes)
            {
                if(ExistingHashes.ContainsKey(pair.Key))
                {
                    ExistingHashes[pair.Key] = pair.Value;
                }
                else
                {
                    ExistingHashes.Add(pair.Key, pair.Value);
                }
            }
        }

        public void EnsureValidTarget(bool createIfMissing = true)
        {
            if (!IsValidTarget(createIfMissing))
            {

            }
        }

        public bool IsValidTarget(bool createIfMissing = true)
        {
            TargetDirectory.Refresh();
            if (!TargetDirectory.Exists)
            {
                if (createIfMissing)
                {
                    try
                    {
                        TargetDirectory.Create();
                        return true; // If we were able to create the directory, it should be valid
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (IOException)
                    { return false; }
#pragma warning restore CA1031 // Do not catch general exception types
                }
                else
                {
                    return false;
                }
            }
            try
            {
                var testDir = TargetDirectory.CreateSubdirectory("dirTest");
                testDir.Refresh();
                if (testDir.Exists)
                {
                    testDir.Delete();
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
            return false;

        }

        public async Task<bool> TransferSong(SongDownload song, bool overwrite, Action<int> ProgressPercent, CancellationToken cancellationToken)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null for LocalDirectoryTarget.TransferSong");
            var createdFiles = new List<FileInfo>();
            bool cancelled = false;
            bool targetDirCreated = false;
            if (string.IsNullOrEmpty(song.LocalDirectory))
                throw new ArgumentException("song's LocalDirectory directory cannot be null for LocalDirectoryTarget.TransferSong", nameof(song));
            var sourceDir = new DirectoryInfo(song.LocalDirectory);
            var targetDir = new DirectoryInfo(Path.Combine(TargetDirectory.FullName, sourceDir.Name));
            if (!targetDir.Exists)
            {
                targetDir.Create();
                targetDirCreated = true;
            }
            if (!sourceDir.Exists)
                throw new ArgumentException(string.Format("source directory does not exist: {0}", song), nameof(song));
            bool success = true;
            var files = sourceDir.EnumerateFiles().Select(f => f.FullName);
            int fileCount = files.Count();
            int fileNum = 1;
            foreach (string sourceFile in files)
            {

                if (!success)
                {
                    success = false;
                    break;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    success = false;
                    break;
                }

                var destFile = new FileInfo(Path.Combine(targetDir.FullName, sourceFile.Substring(sourceFile.LastIndexOf('\\') + 1)));
                if (!overwrite && destFile.Exists)
                    continue;
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
                ProgressPercent?.Invoke(fileNum * 100 / fileCount);
                fileNum++;
            }
            if (success)
            {
                // Check files are valid?

            }
            else // Copy failed, clean up
            {
                ProgressPercent?.Invoke(100);
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
            if (cancelled)
                cancellationToken.ThrowIfCancellationRequested();
            return success;
        }

        public async Task<Dictionary<string, bool>> TransferSongs(IEnumerable<SongDownload> songs, bool overwrite, Action<string, int> SongProgressPercent, CancellationToken cancellationToken)
        {
            var retDict = new Dictionary<string, bool>();
            //var dir = new DirectoryInfo(songs);
            foreach (var song in songs)
            {
                
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (!Directory.EnumerateFiles(song.LocalDirectory, "info.dat", SearchOption.TopDirectoryOnly).Any())
                    continue; // Not a valid song folder, skip
                Action<int> songProgress = null;
                if (SongProgressPercent != null)
                {
                    songProgress = new Action<int>(p => SongProgressPercent(Path.GetFullPath(song.LocalDirectory), p));
                }

                retDict.Add(songDir.FullName, await TransferSong(songDir.FullName, overwrite, songProgress, cancellationToken).ConfigureAwait(false));
            }
            return retDict;
        }

        #region TransferSong Overloads
        public Task<bool> TransferSong(SongDownload songs, Action<int> SongProgressPercent, CancellationToken cancellationToken)
        {
            return TransferSong(songs, false, SongProgressPercent, cancellationToken);
        }

        public Task<bool> TransferSong(SongDownload songs, bool overwrite, CancellationToken cancellationToken)
        {
            return TransferSong(songs, overwrite, null, cancellationToken);
        }

        public Task<bool> TransferSong(SongDownload songs, bool overwrite)
        {
            return TransferSong(songs, overwrite, null, CancellationToken.None);
        }

        public async Task<bool> TransferSong(SongDownload songs)
        {
            var test = await TransferSong(songs, false, null, CancellationToken.None).ConfigureAwait(false);
            return test;
        }
        #endregion

        #region TransferSongs Overloads
        public Task<Dictionary<string, bool>> TransferSongs(IEnumerable<SongDownload> songs, Action<string, int> SongProgressPercent, CancellationToken cancellationToken)
        {
            return TransferSongs(songs, false, SongProgressPercent, cancellationToken);
        }

        public Task<Dictionary<string, bool>> TransferSongs(IEnumerable<SongDownload> songs, bool overwrite, CancellationToken cancellationToken)
        {
            return TransferSongs(songs, overwrite, null, cancellationToken);
        }

        public Task<Dictionary<string, bool>> TransferSongs(IEnumerable<SongDownload> songs, bool overwrite)
        {
            return TransferSongs(songs, overwrite, null, CancellationToken.None);
        }

        public Task<Dictionary<string, bool>> TransferSongs(IEnumerable<SongDownload> songs)
        {
            return TransferSongs(songs, false, null, CancellationToken.None);
        }

        #endregion
    }
}
