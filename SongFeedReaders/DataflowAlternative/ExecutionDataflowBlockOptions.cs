using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SongFeedReaders.DataflowAlternative
{
    public class ExecutionDataflowBlockOptions
    {
        public int MaxDegreeOfParallelism { get; set; }
        public int BoundedCapacity { get; set; }
        public bool EnsureOrdered { get; set; }
        private CancellationToken _cancellationToken = CancellationToken.None;
        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationToken;
            }
            set
            {
                _cancellationToken = value;
            }
        }
    }
}
