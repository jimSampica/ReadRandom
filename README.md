# ReadRandom

Dotnet library for reading random numbers from a CPU's digital random number generator (DRNG).

This library contains methods for invoking the `RDRAND` and `RDSEED` instructions on Intel and AMD CPUs.

See https://www.intel.com/content/www/us/en/developer/articles/guide/intel-digital-random-number-generator-drng-software-implementation-guide.html for more details

![Digital Random Number Generator design](https://raw.githubusercontent.com/jamesSampica/ReadRandom/main/drng.jpg "Digital Random Number Generator design")

# Installing

Install the `Wetware.ReadRandom` package from NuGet (https://www.nuget.org/packages/Wetware.ReadRandom)

# Usage

    using Wetware.ReadRandom;
    
    // Read a random seed value from the CPU
    uint seed = Cpu.ReadRandomSeed();

    // Read a random value from the CPU
    uint random = Cpu.ReadRandom();

# Limitations

- Requires a Windows x64 process. Values are 32-bit unsigned integers.
- Checks CPU support before executing either instruction. Use `Cpu.IsRdRandSupported` and `Cpu.IsRdSeedSupported` to check availability; unsupported calls throw `PlatformNotSupportedException`.
- Reads retry until the CPU reports success, so they have no fixed timeout.
