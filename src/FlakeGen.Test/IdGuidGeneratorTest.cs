using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace FlakeGen.Test
{
    using System.Diagnostics;

    [TestClass]
    public class IdGuidGeneratorTest
    {
        private const int HowManyIds = 10000;
        private const int HowManyThreads = 10;

        [TestMethod]
        public void NewGuidSpeedTest()
        {
            // warm up
            for (int i = 0; i < 128 * 1024; i++)
            {
                Guid guid = Guid.NewGuid();
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 128*1024*1024; i++)
            {
                Guid guid = Guid.NewGuid();
            }
            stopwatch.Stop();

            Console.WriteLine("{0} guids/sec", 1.0 * 128 * 1024 * 1024 / stopwatch.Elapsed.TotalSeconds);
        }

        [TestMethod]
        public void UniqueGuidIds()
        {
            IIdGenerator<Guid> idGenerator = new IdGuidGenerator();

            Guid[] ids = idGenerator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreUnique(ids), "All ids needs to be unique");
        }

        [TestMethod]
        public void SortableGuidIds()
        {
            IIdGenerator<Guid> idGenerator = new IdGuidGenerator();

            Guid[] ids = idGenerator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreSorted(ids), "Ids array needs to be ordered");
        }

        [TestMethod]
        public void DistinctGuidIdsForMultiThreads()
        {
            Thread[] threads = new Thread[HowManyThreads];
            Guid[][] ids = new Guid[HowManyThreads][];
            List<Guid> allIds = new List<Guid>(HowManyIds * HowManyThreads);

            IIdGenerator<Guid> idGenerator = new IdGuidGenerator();

            for (int i = 0; i < HowManyThreads; i++)
            {
                var threadId = i;
                threads[i] = new Thread(() =>
                {
                    ids[threadId] = idGenerator.Take(HowManyIds).ToArray();
                });
                threads[i].Start();
            }

            for (int i = 0; i < HowManyThreads; i++)
            {
                threads[i].Join();

                Assert.IsTrue(AssertUtil.AreUnique(ids[i]), "All ids needs to be unique");
                Assert.IsTrue(AssertUtil.AreSorted(ids[i]), "Ids array needs to be ordered");

                allIds.AddRange(ids[i]);
            }

            Assert.AreEqual(
                HowManyIds * HowManyThreads, allIds.Distinct().Count(),
                "All ids needs to be unique");
        }

        [TestMethod]
        public void IdentiferUsesLowOrderBytesOfInt64()
        {
            long identifier = 0x0123456789abcdef;

            IIdGenerator<Guid> idGenerator = new IdGuidGenerator(identifier);

            Guid id = idGenerator.GenerateId();
            byte[] bytes = id.ToByteArray();

            Assert.AreEqual((byte)0x45, bytes[8 + 0]);
            Assert.AreEqual((byte)0x67, bytes[8 + 1]);
            Assert.AreEqual((byte)0x89, bytes[8 + 2]);
            Assert.AreEqual((byte)0xab, bytes[8 + 3]);
            Assert.AreEqual((byte)0xcd, bytes[8 + 4]);
            Assert.AreEqual((byte)0xef, bytes[8 + 5]);
        }
    }
}
