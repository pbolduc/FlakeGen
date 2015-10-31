# FlakeGen
Flake ID Generators is a set of decentralized, k-ordered id generation services in C#.  This is a fork of [Flake ID Generators](https://flakeidgenerators.codeplex.com/) on CodePlex.

* FlakeGen.Id64Generator - generates 64-bit ids. The implementation is heavily derivative of Twitter's [Snowflake](https://github.com/twitter/snowflake) written is Scala.

* FlakeGen.IdGuidGenerator - generated Guid (128-bit) ids

* FlakeGen.IdStringGeneratorWrapper - a wrapper for FlakeGen.Id64Generator returning k-ordered ids in various string formats (e.g. number, hex, base 32)

Both services generates k-ordered ids (read time-ordered lexically). Run one on each node in your infrastructure and they will generate conflict-free ids on-demand without coordination. 

## Features

**Note**: The id generators currently will throw [InvalidOperationException](https://msdn.microsoft.com/en-us/library/system.invalidoperationexception(v=vs.110).aspx) if the computer's clock is adjusted backwards. This is really only a problem if a computer's time has drifted forward and then is corrected by a NTP clock synchronization. As the id generators always use UTC dates, daylight savings adjustments do not impact id generation.  I plan to add an option to pause the generation of the id until clock has advanced past the previous time.

### FlakeGen.Id64Generator

* Generator written in C#
* Compact ids (under 64 bit) directly sortable
* Id is composed of:
    * **time - 41 bits** (millisecond precision)
    * **configured instance id - 10 bits** - gives us up to 1024 instances
    * **sequence number - 12 bits** - usually 0, incremented when more than one id is requested in the same millisecond and reset to 0 when the clock ticks forward. Rolls over every 4096 per machine.
	* The generator has protection mechanism for sequence roll over in the same millisecond.

### FlakeGen.IdGuidGenerator

* Generator written in C#
* Garbage collector friendly. Generating id values allocates no memory except for the id value.
* Generated Guid style ids directly sortable
* Thread-safe - simple lock is used around generation of id values
* Id is composed of:
    * **time - 64-bits** - the thousands of milliseconds since the epoch. Epoch is customizable. The default epoch is 1970-01-01T00:00:00.000Z.
    * **configured instance id - 48 bits** - it can be MAC address from a configurable device or database sequence number or any other 6 bytes identifier
    * **sequence number - 16-bits** - usually 0, incremented when more than one id is requested in the same millisecond and reset to 0 when the clock ticks forward. Rolls over every 65536 per machine.  No protection for overflow.  A really fast machine would be required to overflow the sequence number. On an [Intel Core i7-3740QM CPU @ 2.70 GHz](https://www.cpubenchmark.net/cpu.php?cpu=Intel+Core+i7-3740QM+%40+2.70GHz&id=1481), approximately 17,000,000 id values can be generated per second. It would take 65,536,000,000 id/second before an overflow would occur.  This is 3800x faster than an [Intel Core i7-3740QM CPU @ 2.70 GHz](https://www.cpubenchmark.net/cpu.php?cpu=Intel+Core+i7-3740QM+%40+2.70GHz&id=1481) can generate id values.

## Usage Guidlines

* Create a single instance per process. It is possible for different instances to create the same id value.
* Ensure different instances use a different instance id.  Examples of creating instance id values for Guid Id Generator:

			// generate a cryptographically strong random identifier
			// you would have to be super unlucky to get a collision
            byte[] identifier = new byte[6];
            RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
            random.GetBytes(identifier);

			// use the MAC address of the first up NIC
			// multiple apps on the same machine would cause a collision,
			// ie Azure Web Apps+Web Jobs or IIS and Windows Services
			byte[] identifier = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .First()
				.GetPhysicalAddress()
				.GetAddressBytes();
