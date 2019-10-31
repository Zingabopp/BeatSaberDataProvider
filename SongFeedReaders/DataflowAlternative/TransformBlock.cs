using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public CancellationToken CancellationToken { get; private set; }
        public bool EnsureOrdered { get; set; }
        private bool Completed { get; set; }
        public Task Completion { get; private set; }

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

        public int InputCount
        {
            get
            {
                return waitQueue.Count;
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
            CancellationToken = CancellationToken.None;
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
            CancellationToken = options.CancellationToken;
            CancellationToken.Register(() => Complete());
        }

        private void QueueNext()
        {
            if (waitQueue.Any())
            {
                // While waitQueue has inputs and running tasks < MaxDegreeOfParallelism
                while (waitQueue.Any() && (taskQueue.Count - OutputCount < MaxDegreeOfParallelism))
                {
                    lock (taskQueueLock)
                    {
                        if (waitQueue.TryDequeue(out var input))
                        {
                            var task = blockFunction(input);
                            task.ContinueWith(OnTaskFinished);
                            taskQueue.Enqueue(task);
                        }
                    }
                }
            }
        }

        private void OnTaskFinished(Task<TOutput> taskResult)
        {
            Task.Run(() => QueueNext());
        }

        /// <summary>
        /// TODO: Make it respect BoundedCapacity (better). Right now if you wait for SendAsync without 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(TInput input, CancellationToken cancellationToken)
        {
            using (var tcs = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken))
            {
                if (Completed || tcs.IsCancellationRequested)
                    return false;
                if (!(await Utilities.WaitUntil(() =>
                 {
                     return (waitQueue.Count + taskQueue.Count) < BoundedCapacity;
                 }, tcs.Token).ConfigureAwait(false)))
                    return false;
            }
            waitQueue.Enqueue(input);
            QueueNext();
            return true;
        }

        public Task<bool> SendAsync(TInput input)
        {
            return SendAsync(input, CancellationToken.None);
        }

        /// <summary>
        /// Waits for an output to become available and returns true.
        /// If there are no workers in the queue, returns false.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<bool> OutputAvailableAsync(CancellationToken cancellationToken)
        {
            if (!(taskQueue.Any() || waitQueue.Any()))
                return false;

            // Finished task is not ready, if there are tasks running or waiting, wait for a finished task
            while (taskQueue.Count > 0 || waitQueue.Any())
            {
                if (taskQueue.TryPeek(out var firstTask))
                {
                    // Wait until first task in the taskQueue is finished
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await firstTask.ConfigureAwait(false);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception) { }
#pragma warning restore CA1031 // Do not catch general exception types
                    return true;
                }
                if (waitQueue.Any())
                    QueueNext(); // Just in case, probably no reason to have this
            }
            return false;
        }

        public Task<bool> OutputAvailableAsync()
        {
            return OutputAvailableAsync(CancellationToken.None);
        }

        /// <summary>
        /// Attempts to retreive a completed worker's result.
        /// </summary>
        /// <param name="output"></param>
        /// <exception cref="Exception">Throws exceptions from the worker.</exception>
        /// <returns></returns>
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
                            if (retTask.IsFaulted)
                                if (retTask.Exception.InnerExceptions.Count == 1)
                                    throw retTask.Exception.InnerException;
                                else
                                    throw retTask.Exception;
                            output = retTask.Result;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// If outputs are available, returns them as a list of BlockResult<<see cref="TOutput"/>>. If an uncaught exception was thrown, it is stored in BlockResult and the Output is null.
        /// </summary>
        /// <param name="outputs"></param>
        /// <returns></returns>
        public bool TryReceiveAll(out IList<BlockResult<TOutput>> outputs)
        {
            outputs = new List<BlockResult<TOutput>>();
            bool hasResult = false;
            bool wasReceived = false;
            do
            {
                wasReceived = false;
                try
                {
                    wasReceived = TryReceive(out var output);
                    if (wasReceived)
                    {
                        hasResult = true;
                        outputs.Add(new BlockResult<TOutput>(output));
                    }
                }
                catch (Exception ex)
                {
                    wasReceived = true;
                    hasResult = true;
                    outputs.Add(new BlockResult<TOutput>(default(TOutput), ex));
                }
            } while (wasReceived);
            return hasResult;
        }

        /// <summary>
        /// Don't accept anymore inputs.
        /// </summary>
        public void Complete()
        {
            if (Completed)
                return;
            Completion = Task.Run(async () =>
            {
                while (InputCount > 0) await Task.Delay(25).ConfigureAwait(false);
                while (OutputCount != taskQueue.Count) await Task.Delay(25).ConfigureAwait(false);
            });
        }
    }
}
