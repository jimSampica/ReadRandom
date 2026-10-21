using System;
using Wetware.ReadRandom;

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
                instruction = Cpu.RdRandInternal;
                read = Cpu.ReadRandom;
                break;
            case "rdseed":
                instruction = Cpu.RdSeedInternal;
                read = Cpu.ReadRandomSeed;
                break;
            default:
                return 2;
        }

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
