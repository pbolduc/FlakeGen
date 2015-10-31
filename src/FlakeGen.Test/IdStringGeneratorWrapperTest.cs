using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace FlakeGen.Test
{
    [TestClass]
    public class IdStringGeneratorWrapperTest
    {
        private const int HowManyIds = 15000;

        private class FakeIdGenerator : IIdGenerator<long>
        {
            private int index = 0;
            private long[] fakeIds;

            public FakeIdGenerator(params long[] fakeIds)
            {
                this.fakeIds = new long[fakeIds.Length];
                Array.Copy(fakeIds, this.fakeIds, fakeIds.Length);
            }
            public long GenerateId()
            {
                if (index == fakeIds.Length)
                    index = 0;

                return fakeIds[index++];
            }

            public System.Collections.Generic.IEnumerator<long> GetEnumerator()
            {
                while (true)
                {
                    yield return GenerateId();
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [TestMethod]
        public void VerifyToStringDefaul()
        {
            var generator = new IdStringGeneratorWrapper(new FakeIdGenerator(5416969582936064L));

            Assert.AreEqual("5416969582936064", generator.GenerateId());
        }

        [TestMethod]
        public void VerifyBase32()
        {
            var generator = new IdStringGeneratorWrapper(
                new FakeIdGenerator(0L, 1L, 10L, 63L, 64L, 0x7FFFFFFFFFFFFFFFL),
                IdStringGeneratorWrapper.Base32);

            Assert.AreEqual("0", generator.GenerateId());
            Assert.AreEqual("1", generator.GenerateId());
            Assert.AreEqual("A", generator.GenerateId());
            Assert.AreEqual("1V", generator.GenerateId());
            Assert.AreEqual("20", generator.GenerateId());
            Assert.AreEqual("7VVVVVVVVVVVV", generator.GenerateId());
        }

        [TestMethod]
        public void VerifyBase32WithLeadingZero()
        {
            var generator = new IdStringGeneratorWrapper(
                new FakeIdGenerator(0L, 1L, 10L, 63L, 64L, 0x7FFFFFFFFFFFFFFFL),
                IdStringGeneratorWrapper.Base32LeadingZero);

            Assert.AreEqual("0000000000000", generator.GenerateId());
            Assert.AreEqual("0000000000001", generator.GenerateId());
            Assert.AreEqual("000000000000A", generator.GenerateId());
            Assert.AreEqual("000000000001V", generator.GenerateId());
            Assert.AreEqual("0000000000020", generator.GenerateId());
            Assert.AreEqual("7VVVVVVVVVVVV", generator.GenerateId());
        }

        [TestMethod]
        public void VerifyToStringDefaulWithPrefix()
        {
            var generator = new IdStringGeneratorWrapper(new FakeIdGenerator(5416969582936001L), "x");

            Assert.AreEqual("x5416969582936001", generator.GenerateId());
        }

        [TestMethod]
        public void VerifyUpperHex()
        {
            var generator = new IdStringGeneratorWrapper(
                new FakeIdGenerator(0x7f23f22ffcdffff1L),
                IdStringGeneratorWrapper.UpperHex);

            Assert.AreEqual("7F23F22FFCDFFFF1", generator.GenerateId());
        }

        [TestMethod]
        public void VerifyLoweHex()
        {
            var generator = new IdStringGeneratorWrapper(
                new FakeIdGenerator(0x7f23f22ffcdfffc1L),
                IdStringGeneratorWrapper.LowerHex);

            Assert.AreEqual("7f23f22ffcdfffc1", generator.GenerateId());
        }

        [TestMethod]
        public void UniqueDefaultToStringIds()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(new Id64Generator());

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreUnique(ids), "Generated ids needs to be unique");
        }

        [TestMethod]
        public void UniqueUpperHexIds()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.UpperHex);

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreUnique(ids), "Generated ids needs to be unique");
        }

        [TestMethod]
        public void UniqueLowerHexIds()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.LowerHex);

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreUnique(ids), "Generated ids needs to be unique");
        }

        [TestMethod]
        public void SortedDefaultToStringIds()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(new Id64Generator());

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreSorted(ids), "Generated ids needs to be sorted");
        }

        [TestMethod]
        public void SortedUpperHexIds()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.UpperHex);

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreSorted(ids), "Generated ids needs to be sorted");
        }

        [TestMethod]
        public void SortedLowerHexIds()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.LowerHex);

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreSorted(ids), "Generated ids needs to be sorted");
        }

        [TestMethod]
        public void UniqueBase64Ids()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.Base32);

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreUnique(ids), "Generated ids needs to be unique");
        }

        [TestMethod]
        public void SortedBase64Ids()
        {
            IIdGenerator<string> generator
                = new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.Base32);

            var ids = generator.Take(HowManyIds).ToArray();

            Assert.IsTrue(AssertUtil.AreSorted(ids), "Generated ids needs to be sorted");
        }
    }
}
