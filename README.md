# FlakeGen
Flake ID Generators is a set of decentralized, k-ordered id generation services in C#.  This is a fork of [Flake ID Generators](https://flakeidgenerators.codeplex.com/) on CodePlex.

* FlakeGen.Id64Generator - generates 64-bit ids. The implementation is heavily derivative of Twitter's [Snowflake](https://github.com/twitter/snowflake) written is Scala.

* FlakeGen.IdGuidGenerator - generated Guid (128-bit) ids

* FlakeGen.IdStringGeneratorWrapper - a wrapper for FlakeGen.Id64Generator returning k-ordered ids in various string formats (e.g. number, hex, base 32)

Both services generates k-ordered ids (read time-ordered lexically). Run one on each node in your infrastructure and they will generate conflict-free ids on-demand without coordination. 

## Features

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
* Generated Guid style ids directly sortable
* Thread-safe - simple lock is used around generation of id values
* Id is composed of:
    * **time - 64-bits** - the UTC milliseconds since the epoch (Jan 1 1970)
    * **configured instance id - 48 bits** - it can be MAC address from a configurable device or database sequence number or any other 6 bytes identifier
    * **sequence number - 16-bits** - usually 0, incremented when more than one id is requested in the same millisecond and reset to 0 when the clock ticks forward. Rolls over every 65536 per machine.  No protection for overflow.  A really fast machine would be required to overflow the sequence number. On a [Intel Core i7-3740QM CPU @ 2.70 GHz](https://www.cpubenchmark.net/cpu.php?cpu=Intel+Core+i7-3740QM+%40+2.70GHz&id=1481), approximately 16m id values can be generated per second. It would take 65,536,000 id/second before an overflow would occur.
	
## Usage Guidlines

* Create a single instance per process
