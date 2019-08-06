using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongFeedReaders.DataflowAlternative
{
    public class TransformBlock<TInput, TOutput>
    {
        private object taskQueueLock = new object();
        private ConcurrentQueue<Task<TOutput>> taskQueue;
        private ConcurrentQueue<TInput> waitQueue;
        public int BoundedCapacity { get; private set; }
        public int MaxDegreeOfParallelism { get; private set; }
        public bool EnsureOrdered { get; set; }

        private Func<TInput, Task<TOutput>> blockFunction;

        public int OutputCount
        {
            get
            {
                int count = 0;
                lock (taskQueueLock)
                {
                    count = taskQueue.Where(t => t.IsCompleted).Count();
                }
                return count;
            }
        }

        public TransformBlock(Func<TInput, Task<TOutput>> function)
        {
            taskQueue = new ConcurrentQueue<Task<TOutput>>();
            waitQueue = new ConcurrentQueue<TInput>();
            blockFunction = function;
            BoundedCapacity = 1;
            MaxDegreeOfParallelism = 1;
            EnsureOrdered = true;
        }

        public TransformBlock(Func<TInput, Task<TOutput>> function, ExecutionDataflowBlockOptions options)
            : this(function)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options), "options cannot be null for TransformBlock's constructor");
            taskQueue = new ConcurrentQueue<Task<TOutput>>();
            waitQueue = new ConcurrentQueue<TInput>();
            blockFunction = function;
            BoundedCapacity = options.BoundedCapacity;
            MaxDegreeOfParallelism = options.MaxDegreeOfParallelism;
        }

        public async Task<bool> SendAsync(TInput input)
        {
            bool wasAdded = false;
            if(taskQueue.Count() < MaxDegreeOfParallelism)
            {
                taskQueue.Enqueue(Task.Run(() => blockFunction(input)));
            }
            return input != null;
        }

        public async Task<bool> OutputAvailableAsync()
        {
            while (taskQueue.Count > 0)
            {
                if (taskQueue.TryPeek(out var firstTask))
                {
                    await firstTask.ConfigureAwait(false);
                    return true;
                }
            }
            return false;
        }

        public bool TryReceive(out TOutput output)
        {
            output = default(TOutput);
            if (taskQueue.Count == 0)
                return false;
            lock (taskQueueLock)
            {
                if (taskQueue.TryPeek(out var task))
                {
                    if (task.IsCompleted)
                    {
                        if (taskQueue.TryDequeue(out var retTask))
                        {
                            output = retTask.Result;
                            return true;
                        }

                    }
                }
            }
            return false;
        }

        public bool TryReceiveAll(out IList<TOutput> outputs)
        {
            outputs = new List<TOutput>();
            bool hasResult = false;
            while (TryReceive(out var output))
            {
                hasResult = true;
                outputs.Add(output);
            }
            return hasResult;
        }

        /// <summary>
        /// Don't accept anymore inputs.
        /// </summary>
        public void Complete()
        {

        }
    }
}
