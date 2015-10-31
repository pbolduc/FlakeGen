using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FlakeGen.Test
{
    [TestClass]
    public class IdGuidGeneratorTest
    {
        private const int HowManyIds = 10000;
        private const int HowManyThreads = 10;

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
    }
}
