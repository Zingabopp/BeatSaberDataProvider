using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongDownloadManager
{
    /// <summary>
    /// Interface for transferring downloaded songs to a target.
    /// </summary>
    public interface ISongDownloadTarget
    {
        /// <summary>
        /// Unique ID of the ISongDownloadTarget. There should only be one ISongDownloadTarget to a path.
        /// </summary>
        string ID { get; }
        /// <summary>
        /// Name of the download target.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Description of the download target.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Asynchronously retrieves a list of existing song hashes from the target. If hashExisting is true, hash songs that have been previously hashed.
        /// Returns null if not supported.
        /// </summary>
        /// <returns></returns>
        Task<List<string>> GetExistingSongHashesAsync(bool hashExisting);

        /// <summary>
        /// Asynchronously retrieves a list of existing song hashes from the target. Returns null if not supported.
        /// </summary>
        /// <returns></returns>
        Task<List<string>> GetExistingSongHashesAsync();

        /// <summary>
        /// Throws an exception if the target is invalid.
        /// </summary>
        /// <param name="createIfMissing"></param>
        void EnsureValidTarget(bool createIfMissing = true);

        /// <summary>
        /// Returns true if the target is valid. If createIfMissing is true, attempt to make the target valid.
        /// </summary>
        /// <param name="createIfMissing"></param>
        /// <returns></returns>
        bool IsValidTarget(bool createIfMissing = true);

        /// <summary>
        /// Asynchronously transfers the specified source to the target. Return true if transfer was successful.
        /// </summary>
        /// <param name="source">Song source path</param>
        /// <param name="overwrite">Overwrite if song exists at the target location</param>
        /// <param name="ProgressPercent">Song transfer progress as a percentage</param>
        /// <param name="cancellationToken">Token to trigger a cancellation for an in-progress transfer.</param>
        /// <returns></returns>
        Task<bool> TransferSong(string source, bool overwrite, Action<int> ProgressPercent, CancellationToken cancellationToken);
        Task<bool> TransferSong(string source, Action<int> ProgressPercent, CancellationToken cancellationToken);
        Task<bool> TransferSong(string source, bool overwrite, CancellationToken cancellationToken);
        Task<bool> TransferSong(string source, bool overwrite);
        Task<bool> TransferSong(string source);


        /// <summary>
        /// Asynchronously transfer all songs in the specified sourceDirectory to the target.
        /// Returns a Dictionary with the song hash as the key and whether the transfer was successful as the value.
        /// </summary>
        /// <param name="sourceDirectory">Songs source directory</param>
        /// <param name="overwrite">Overwrite if a song exists at the target location</param>
        /// <param name="SongProgressPercent">Individual song progress: string is the song hash, int is the song progress percentage</param>
        /// <param name="cancellationToken">Token to trigger a cancellation all remaining transfers.</param>
        /// <returns></returns>
        Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, bool overwrite, Action<string, int> SongProgressPercent, CancellationToken cancellationToken);
        Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, Action<string, int> SongProgressPercent, CancellationToken cancellationToken);
        Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, bool overwrite, CancellationToken cancellationToken);
        Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory, bool overwrite);
        Task<Dictionary<string, bool>> TransferSongs(string sourceDirectory);

    }
}
