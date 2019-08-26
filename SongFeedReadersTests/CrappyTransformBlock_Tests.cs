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
            for (int i = 0; i < 5; i++)
                Assert.IsTrue(outputs[i].Contains(i.ToString()));
        }

        [TestMethod]
        public void SingleThreaded_CorrectOrder()
        {
            var numInputs = 50;
            var boundedCapacity = numInputs;
            var maxDegreeParallelism = 1;
            var block = new TransformBlock<int, string>(async i =>
            {
                Console.WriteLine("Starting block with input: " + i);
                await Task.Delay(100).ConfigureAwait(false);

                await Task.Delay(100 / (i * 2 + 1)).ConfigureAwait(false);
                return $"{i} completed";
            }, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = boundedCapacity,
                MaxDegreeOfParallelism = maxDegreeParallelism
            }
            );

            for (int i = 0; i < numInputs; i++)
            {
                block.SendAsync(i).Wait();
            }
            int received = 0;
            var outputs = new List<string>();
            while (received < numInputs && block.InputCount + block.OutputCount > 0)
            {
                block.OutputAvailableAsync().Wait();
                if (block.TryReceive(out var output))
                {
                    received++;
                    outputs.Add(output);
                }
            }
            Assert.IsTrue(numInputs == outputs.Count);
            for (int i = 0; i < numInputs; i++)
                Assert.IsTrue(outputs[i].Contains(i.ToString()));
        }


    }
}
