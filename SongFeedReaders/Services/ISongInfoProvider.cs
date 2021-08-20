using SongFeedReaders.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.Services
{
    public interface ISongInfoProvider
    {
        /// <summary>
        /// 
        /// </summary>
        int Priority { get; set; }
        /// <summary>
        /// Service is available.
        /// </summary>
        bool Available { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="SongInfoProviderException"></exception>
        Task<ScrapedSong?> GetSongByHashAsync(string hash, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="SongInfoProviderException"></exception>
        Task<ScrapedSong?> GetSongByKeyAsync(string key, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="SongInfoProviderException"></exception>
        Task<ScrapedSong?> GetSongByHashAsync(string hash);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="SongInfoProviderException"></exception>
        Task<ScrapedSong?> GetSongByKeyAsync(string key);
    }

    /// <summary>
    /// Contains Exceptions thrown by ISongInfoProvider.
    /// </summary>
    [Serializable]
    public class SongInfoProviderException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="SongInfoProviderException"/>.
        /// </summary>
        public SongInfoProviderException() : base()
        {
        }

        /// <summary>
        /// Creates a new <see cref="SongInfoProviderException"/> with a message.
        /// </summary>
        public SongInfoProviderException(string message) : base(message)
        {
        }
        /// <summary>
        /// Creates a new <see cref="SongInfoProviderException"/> with a message and inner exception.
        /// </summary>
        public SongInfoProviderException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected SongInfoProviderException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
