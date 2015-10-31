using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FlakeGen.Test
{
    [TestClass]
    public class Id64GeneratorTest
    {
        private const int HowManyIds = 10000;
        private const int HowManyThreads = 10;

        [TestMethod]
        public void UniqueLongIds()
        {
            IIdGenerator<long> idGenerator = new Id64Generator();

            long[] ids = idGenerator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreUnique(ids), "All ids needs to be unique");
        }

        [TestMethod]
        public void SortableLongIds()
        {
            IIdGenerator<long> idGenerator = new Id64Generator();

            long[] ids = idGenerator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreSorted(ids), "Ids array needs to be ordered");
        }

        [TestMethod]
        public void DistinctLongIdsForMultiThreads()
        {
            Thread[] threads = new Thread[HowManyThreads];
            long[][] ids = new long[HowManyThreads][];
            List<long> allIds = new List<long>(HowManyIds * HowManyThreads);

            IIdGenerator<long> idGenerator = new Id64Generator();

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
