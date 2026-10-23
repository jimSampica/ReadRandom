using System;
using System.Threading.Tasks;
using ReadRandom;
using Xunit;

namespace Tests;

// Run native code in a child process so access violations cannot kill the test runner.
internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "--hardware-probe")
            return 2;

        CpuReader.ReadInstruction instruction;
        Func<uint> read;
        switch (args[1])
        {
            case "rdrand":
                Assert.True(Cpu.IsRdRandSupported);
                instruction = Cpu.RdRandInternal;
                read = Cpu.ReadRandom;
                break;
            case "rdseed":
                Assert.True(Cpu.IsRdSeedSupported);
                instruction = Cpu.RdSeedInternal;
                read = Cpu.ReadRandomSeed;
                break;
            case "unsupported":
                Assert.False(Cpu.IsRdRandSupported);
                Assert.False(Cpu.IsRdSeedSupported);
                Assert.Throws<PlatformNotSupportedException>(() => Cpu.ReadRandom());
                Assert.Throws<PlatformNotSupportedException>(() => Cpu.ReadRandomSeed());
                Assert.Throws<PlatformNotSupportedException>(() => Cpu.RdRandInternal(out _));
                Assert.Throws<PlatformNotSupportedException>(() => Cpu.RdSeedInternal(out _));
                Console.WriteLine("Hardware probe passed.");
                return 0;
            default:
                return 2;
        }

        // Concurrent first reads must share a fully initialized instruction buffer.
        Parallel.For(0, 32, _ => { read(); });

        // The executable allocation must remain valid after managed objects are collected.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var succeeded = false;
        for (var attempt = 0; attempt < 10000; attempt++)
        {
            var value = instruction(out var carry);
            if (carry > 1)
                throw new InvalidOperationException($"Invalid carry flag: {carry}.");
            if (carry == 0 && value != 0)
                throw new InvalidOperationException("A failed instruction must return zero.");
            if (carry == 1)
            {
                succeeded = true;
                break;
            }
        }

        if (!succeeded)
            throw new InvalidOperationException("No successful instruction in 10000 attempts.");

        // Every uint, including zero, is valid. Verify completion, not a distribution.
        for (var sample = 0; sample < 32; sample++)
            _ = read();

        Console.WriteLine("Hardware probe passed.");
        return 0;
    }
}
