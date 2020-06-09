using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.DataflowAlternative
{
    public class BlockResult<TOutput>
    {
        public BlockResult(TOutput output, Exception? exception = null)
        {
            Output = output;
            Exception = exception;
        }

        public TOutput Output { get; private set; }
        public Exception? Exception { get; private set; }
    }
}
