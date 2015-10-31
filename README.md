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
    * **time - 64-bits** - the 1/10,000th of a millisecond since the epoch. Epoch is customizable. The default epoch is 1970-01-01T00:00:00.000Z.
    * **configured instance id - 48 bits** - it can be MAC address from a configurable device or database sequence number or any other 6 bytes identifier
    * **sequence number - 16-bits** - usually 0, incremented when more than one id is requested in the same 64-bit time value and reset to 0 when the 64-bit time value ticks forward. Rolls over every 65536 per machine.  There is **no protection** for overflow.  A really fast machine would be required to overflow the sequence number. On an [Intel Core i7-3740QM CPU @ 2.70 GHz](https://www.cpubenchmark.net/cpu.php?cpu=Intel+Core+i7-3740QM+%40+2.70GHz&id=1481), approximately 19 million id values can be generated per second. It would take 655,360 million (2^16 x 1000 ms/sec x 10000 ticks/ms) id/second before an overflow would occur.  This is over 8000 times faster than an [Intel Core i7-3740QM CPU @ 2.70 GHz](https://www.cpubenchmark.net/cpu.php?cpu=Intel+Core+i7-3740QM+%40+2.70GHz&id=1481) can generate id values.

Generated Guid mapped like following:

	00336254-4f35-8d09-1234-56789abc0004
	\----------------/ \-----------/\--/
	        v                v        v
	       time         instance id  sequence

When was this id generated?
	
	DateTime when = new DateTime(IdGuidGenerator.DefaultEpoch.Ticks + 0x003362544f358d09);
    Console.WriteLine("{0:O}", when); // Approximately: 2015-10-31T23:23:25.7927945  (UTC)

Internally, IdGuidGenerator must divide the clock's ticks by a divisor, otherwise 

## Changes from [Flake ID Generators](https://flakeidgenerators.codeplex.com/) on CodePlex

* Does not allocate memory during Guid id generation
* Faster Guid id generation. Approximately 5 times faster. 19 million/sec vs 3.8 million/sec
* Guid id values generated will not be k-ordered correctly with the original implementation. The time component is now 1000x larger. The original implementation would divide ticks by 10 to generate the time stamp portition of the Guid.  Since ticks is a 64-bit integer, there are no cases that it would overflow the 8 bytes allocated to the time.  Dividing the ticks only slows down the performance. It provides no value.

## Usage Guidlines

* Create a single instance per process. It is possible for different instances to create the same id value.
* Ensure different instances use a different instance id.  

## Examples

### Generate a random identifier

	// generate a cryptographically strong random identifier
	// you would have to be super unlucky to get a collision
	byte[] identifier = new byte[6];
	RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
	random.GetBytes(identifier);
	IdGuidGenerator generator = new IdGuidGenerator(identifier);

### Generate the identifier based on the computer's MAC address

	// use the MAC address of the first up NIC
	// multiple apps on the same machine would cause a collision,
	// ie Azure Web Apps+Web Jobs or IIS and Windows Services
	byte[] identifier = NetworkInterface.GetAllNetworkInterfaces()
		.Where(i => i.OperationalStatus == OperationalStatus.Up)
		.First()
		.GetPhysicalAddress()
		.GetAddressBytes();
	IdGuidGenerator generator = new IdGuidGenerator(identifier);

## Benchmarks

The FlakeGen console will benchmark the Guid Id Generator.  Below is an example of the performance
output.

	== Benchmark Guid ids with 134,217,728 iterations ==
	Elapsed 00:00:08.708 (15,411,853.9 id/sec)
	Elapsed 00:00:08.729 (15,375,365.2 id/sec)
	Elapsed 00:00:08.717 (15,396,094.5 id/sec)
	Elapsed 00:00:08.711 (15,406,082.2 id/sec)
	Elapsed 00:00:08.694 (15,436,761.0 id/sec)
	
and the source code that produced the benchmark.  Any suggestions on improving the benchmark or the performance is welcome.

	private static void BenchmarkGuidGenerator()
	{
		int iterations = 128 * 1024 * 1024;

		Console.WriteLine();
		Console.WriteLine(" == Benchmark Guid ids with {0} iterations ==", iterations);

		for (int i = 0; i < 5; i++)
		{
			var idGuidGenerator = new IdGuidGenerator(0x123456789ABCL);
			TimeSpan elapsed = Benchmark(idGuidGenerator, iterations);
			Console.WriteLine("Elapsed {0} ({1:0.#} id/sec)", elapsed, iterations / elapsed.TotalSeconds);
		}
	}

	private static TimeSpan Benchmark<T>(IIdGenerator<T> generator, int iterations)
	{
		// warm up
		for (int i = 0; i < 1024 * 16; i++)
		{
			generator.GenerateId();
		}

		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int i = 0; i < iterations; i++)
		{
			generator.GenerateId();
		}
		stopwatch.Stop();
		return stopwatch.Elapsed;
	}