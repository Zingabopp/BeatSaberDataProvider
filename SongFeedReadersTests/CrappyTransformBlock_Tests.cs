using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq;
using System.IO;
using SongFeedReaders;
using Newtonsoft.Json.Linq;
using System;
using SongFeedReaders.DataflowAlternative;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SongFeedReadersTests
{
    [TestClass]
    public class CrappyTransformBlockTests
    {
        static CrappyTransformBlockTests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void SingleTaskExceptioned_AsyncFunc()
        {
            var block = new TransformBlock<int, string>(async i =>
            {
                Console.WriteLine("Starting block with input: " + i);
                await Task.Delay(100).ConfigureAwait(false);
                if (i == 2)
                    throw new ArgumentException("i == 2", nameof(i));
                else
                    await Task.Delay(500 / (i * 2 + 1)).ConfigureAwait(false);
                return $"{i} completed";
            }, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 1
            });

            for (int i = 0; i < 5; i++)
            {
                block.SendAsync(i).Wait();
            }
            int received = 0;
            var outputs = new List<string>();
            while (received < 5)
            {
                block.OutputAvailableAsync().Wait();
                if (received == 2)
                {
                    Assert.ThrowsException<ArgumentException>(() => block.TryReceive(out var output));
                    outputs.Add("2 Exception");
                    received++;
                }
                else
                {
                    if (block.TryReceive(out var output))
                    {
                        received++;
                        if (received == 2)
                        {
                            Console.WriteLine("this should've errored");
                        }
                        outputs.Add(output);
                    }
                }
            }
            for (int i = 0; i < 5; i++)
                if (i != 2)
                    Assert.IsTrue(outputs[i].Contains(i.ToString()));
        }

        [TestMethod]
        public void SingleThreaded_WaitCompletion()
        {
            var numInputs = 10;
            var boundedCapacity = numInputs;
            var maxDegreeParallelism = 1;
            var block = new TransformBlock<BlockTestInput, BlockTestOutput>(async i =>
            {
                var startTime = DateTime.Now;
                await Task.Delay(i.TaskDuration);
                return new BlockTestOutput(i, startTime, DateTime.Now);
            }, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = boundedCapacity,
                MaxDegreeOfParallelism = maxDegreeParallelism
            });
            var inputList = new Dictionary<int, BlockTestInput>();
            for (int i = 0; i < numInputs; i++)
            {
                inputList.Add(i, new BlockTestInput(i, new TimeSpan(0, 0, 0, 0, 1)));
            }
            foreach (var item in inputList)
            {
                block.SendAsync(item.Value);
            }
            block.Complete();
            block.Completion().Wait();
            var outputList = new List<BlockTestOutput>();
            while (block.TryReceive(out var output))
            {
                outputList.Add(output);
            }
            Assert.AreEqual(numInputs, outputList.Count);
            var lastId = -1;
            var lastStart = DateTime.MinValue;
            var lastCompletion = DateTime.MinValue;
            foreach (var output in outputList)
            {
                Assert.AreEqual(lastId + 1, output.Input.Id);
                Assert.IsTrue(lastStart < output.TaskStart);
                Assert.IsTrue(lastCompletion < output.TaskFinished);
                lastStart = output.TaskStart;
                lastCompletion = output.TaskFinished;
                lastId++;
            }
        }

        [TestMethod]
        public void SingleThreaded_WaitAvailable()
        {
            var numInputs = 10;
            var boundedCapacity = numInputs;
            var maxDegreeParallelism = 1;
            var block = new TransformBlock<BlockTestInput, BlockTestOutput>(async i =>
            {
                var startTime = DateTime.Now;
                await Task.Delay(i.TaskDuration);
                return new BlockTestOutput(i, startTime, DateTime.Now);
            }, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = boundedCapacity,
                MaxDegreeOfParallelism = maxDegreeParallelism
            });
            var inputList = new Dictionary<int, BlockTestInput>();
            for (int i = 0; i < numInputs; i++)
            {
                inputList.Add(i, new BlockTestInput(i, new TimeSpan(0, 0, 0, 0, 1)));
            }
            foreach (var item in inputList)
            {
                block.SendAsync(item.Value);
            }
            block.Complete();
            block.Completion().Wait();
            var outputList = new List<BlockTestOutput>();
            for (int i = 0; i < numInputs; i++)
            {
                block.OutputAvailableAsync().Wait();
                Assert.IsTrue(block.TryReceive(out var output));
                outputList.Add(output);
            }
            Assert.AreEqual(numInputs, outputList.Count);
            var lastId = -1;
            var lastStart = DateTime.MinValue;
            var lastCompletion = DateTime.MinValue;
            foreach (var output in outputList)
            {
                Assert.AreEqual(lastId + 1, output.Input.Id);
                Assert.IsTrue(lastStart < output.TaskStart);
                Assert.IsTrue(lastCompletion < output.TaskFinished);
                lastStart = output.TaskStart;
                lastCompletion = output.TaskFinished;
                lastId++;
            }
        }
    }

    public class BlockTestInput
    {
        public BlockTestInput() { }
        public BlockTestInput(int id, TimeSpan duration)
        {
            Id = id;
            TaskDuration = duration;
            InputCreated = DateTime.Now;
        }
        public int Id { get; set; }
        public DateTime InputCreated { get; set; }
        public TimeSpan TaskDuration { get; set; }
    }

    public class BlockTestOutput
    {
        public BlockTestOutput() { }
        public BlockTestOutput(BlockTestInput input, DateTime startTime, DateTime endTime)
        {
            Input = input;
            TaskStart = startTime;
            TaskFinished = endTime;
        }
        public BlockTestInput Input { get; set; }
        public DateTime TaskStart { get; set; }
        public DateTime TaskFinished { get; set; }
    }
}
