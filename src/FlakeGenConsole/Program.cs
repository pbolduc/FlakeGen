using FlakeGen;
using System;
using System.Collections;
using System.Linq;
using System.Net.NetworkInformation;

namespace FlakeGenConsole
{
    class Program
    {
        const string formatter = "{0,22}{1,30}";

        static void Main(string[] args)
        {
            Long64BitGenerator();

            GuidGenerator();

            MacAddressGenerator();

            LongToStringIdGenerator();

            LongToUpperHexStringIdGenerator();

            LongToLowerHexStringIdGenerator();

            LongToBase32StringIdGenerator();
        }

        private static void LongToBase32StringIdGenerator()
        {
            Console.WriteLine();
            Console.WriteLine(" == String ids (base 32 conversion) ==");

            var idGenerator =
                new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.Base32);

            foreach (var id in idGenerator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }

            Console.WriteLine();
            Console.WriteLine(" == String ids (base 32 conversion, leading zero) ==");

            idGenerator =
                new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.Base32LeadingZero);

            foreach (var id in idGenerator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }
        }

        private static void LongToLowerHexStringIdGenerator()
        {
            Console.WriteLine();
            Console.WriteLine(" == String ids (lower hex conversion) with prefix 'low' ==");

            var idGenerator =
                new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.LowerHex, "low");

            foreach (var id in idGenerator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }
        }

        private static void LongToUpperHexStringIdGenerator()
        {
            Console.WriteLine();
            Console.WriteLine(" == String ids (upper hex conversion) with prefix 'upper' ==");

            var idGenerator =
                new IdStringGeneratorWrapper(
                    new Id64Generator(), IdStringGeneratorWrapper.UpperHex, "upper");

            foreach (var id in idGenerator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }
        }

        private static void LongToStringIdGenerator()
        {
            Console.WriteLine();
            Console.WriteLine(" == String ids with prefix 'o' ==");

            var idGenerator = new IdStringGeneratorWrapper(new Id64Generator(), "o");

            foreach (var id in idGenerator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }
        }

        private static void Long64BitGenerator()
        {
            Console.WriteLine(" == Long ids (64 bit) ==");

            var id64Generator = new Id64Generator();

            foreach (var id in id64Generator.Take(5).ToArray())
            {
                GetBytesUInt64((ulong)id);
            }
        }

        private static void GuidGenerator()
        {
            Console.WriteLine();
            Console.WriteLine(" == Guid ids ==");

            var idGuidGenerator = new IdGuidGenerator(0x123456789ABCL);

            foreach (var id in idGuidGenerator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }
        }

        public static void MacAddressGenerator()
        {
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .First().GetPhysicalAddress().GetAddressBytes();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(mac);

            var generator = new IdGuidGenerator(mac);

            Console.WriteLine();
            Console.WriteLine(" == Guid ids with MAC Address identifier ({0}) ==",
                BitConverter.ToString(mac));

            foreach (var id in generator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }

            Console.WriteLine();
            Console.WriteLine(" == Guid ids with MAC Address identifier:[{0}] and epoch:[{1}] ==",
                BitConverter.ToString(mac), new DateTime(2012, 10, 1));

            generator = new IdGuidGenerator(mac, new DateTime(2012, 10, 1));

            foreach (var id in generator.Take(5).ToArray())
            {
                Console.WriteLine(id);
            }
        }

        public static void GetBytesUInt64(ulong argument)
        {
            byte[] byteArray = BitConverter.GetBytes(argument);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(byteArray);

            Console.WriteLine(formatter, argument,
                BitConverter.ToString(byteArray));
        }
    }
}
