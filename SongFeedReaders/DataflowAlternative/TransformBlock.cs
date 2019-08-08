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

        public int InputCount { get; private set; }

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
                            taskQueue.Enqueue(Worker(blockFunction(input)));
                    }
                }
            }
        }

        private async Task<TOutput> Worker(Task<TOutput> function)
        {

            TOutput result = await function.ConfigureAwait(false);
            Console.WriteLine($"Finished worker with result {result}");
            InputCount--;
            if (waitQueue.Count > 0)
                QueueNext();
            return result;
        }

        /// <summary>
        /// TODO: Make it respect BoundedCapacity (better). Right now if you wait for SendAsync without 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(TInput input, CancellationToken cancellationToken)
        {
            await Utilities.WaitUntil(() =>
            {
                return (waitQueue.Count + taskQueue.Count) < BoundedCapacity;
            }, cancellationToken).ConfigureAwait(false);
            QueueNext();
            // Check if anything's in the waitQueue so this input doesn't jump the line.
            if (!waitQueue.Any() && taskQueue.Count - OutputCount < MaxDegreeOfParallelism)
            {
                lock (taskQueueLock)
                {
                    taskQueue.Enqueue(Worker(blockFunction(input)));
                }
            }
            else
                waitQueue.Enqueue(input);
            InputCount++;
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
        /// <returns></returns>
        public async Task<bool> OutputAvailableAsync()
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

        public async Task Completion()
        {
            await Task.Run(async () =>
           {
               while (InputCount > 0) await Task.Delay(25).ConfigureAwait(false);
           }).ConfigureAwait(false);
            return;
        }
    }
}
