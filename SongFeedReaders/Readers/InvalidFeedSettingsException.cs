using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.Readers
{
    public class InvalidFeedSettingsException : InvalidOperationException
    {
        public InvalidFeedSettingsException()
            : base() { }
        public InvalidFeedSettingsException(string message)
            : base(message) { }
        public InvalidFeedSettingsException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
