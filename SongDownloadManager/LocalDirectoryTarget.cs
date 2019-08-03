using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberDataProvider.DataModels;

namespace SongDownloadManager
{
    public class LocalDirectoryTarget : ISongDownloadTarget
    {
        public DirectoryInfo Directory { get; private set; }
        public ConcurrentDictionary<string, string> Songs { get; private set; }
        private Dictionary<DirectoryInfo, SongHashData> ExistingHashes { get; set; }

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
            Songs = new ConcurrentDictionary<string, string>();
            Directory = new DirectoryInfo(directory);
            if(!Directory.Exists)
            {
                if (!createIfMissing)
                    throw new ArgumentException(string.Format("Target directory, {0}, does not exist and createIfMissing is false.", directory), nameof(directory));
                Directory.Create();
            }
            Name = string.IsNullOrEmpty(targetName) ? Directory.Name : targetName;
            Description = string.IsNullOrEmpty(targetName) ? Directory.FullName : targetDescription;

            
        }

        public LocalDirectoryTarget(string directory, bool createIfMissing = true)
            : this(null, null, directory, createIfMissing)
        { }

        public async Task<List<string>> GetExistingSongHashesAsync()
        {

            foreach (var songDir in Directory.GetDirectories())
            {

            }
        }

        public Task<bool> TransferSong(string source, bool overwrite, Action<int> ProgressPercent, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        

        public Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, bool overwrite, Action<string, int> SongProgressPercent, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

        public Task<bool> TransferSong(string source)
        {
            return TransferSong(source, false, null, CancellationToken.None);
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
