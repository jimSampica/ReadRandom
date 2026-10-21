using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wetware.ReadRandom;

/// <summary>Contains methods used for reading random numbers generated from the CPU's digital random number generator (DRNG).</summary>
public static class Cpu
{
    static Cpu()
    {
        var rdSeedOps = new byte[] { 
            0x0f, 0xc7, 0xf8, // rdseed eax
            0x0f, 0x92, 0x01, // setc byte ptr [rcx]
            0xc3              // ret
        };

        var rdRandOps = new byte[] { 
            0x0f, 0xc7, 0xf0, // rdrand eax
            0x0f, 0x92, 0x01, // setc byte ptr [rcx]
            0xc3              // ret
        };

        // Override our internal method with our opcodes
        Marshal.Copy(rdSeedOps, 0, typeof(Cpu).GetMethod(nameof(RdSeedInternal))!.MethodHandle.GetFunctionPointer(), rdSeedOps.Length);
        Marshal.Copy(rdRandOps, 0, typeof(Cpu).GetMethod(nameof(RdRandInternal))!.MethodHandle.GetFunctionPointer(), rdRandOps.Length);
    }

    
    /// <summary>Reads a random seed value from the CPU's deterministic random bit generator (DRBG).</summary>
    public static uint ReadRandomSeed() => CpuReader.Read(RdSeedInternal);

    /// <summary>Reads a random value from the CPU's enhanced, nondeterministic random number generator (ENRNG).</summary>
    public static uint ReadRandom() => CpuReader.Read(RdRandInternal);

    /// <summary>Internal instructions for RdSeed. Not to be called directly. Use <see cref="ReadRandomSeed"/> instead.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)] // Don't inline. This function serves as a pointer to override with cpu opcodes
    public static uint RdSeedInternal(out byte carry)
    {
        // Needed to compile
        carry = byte.MaxValue;
        return 0;
    }

    /// <summary>Internal instructions for RdRand. Not to be called directly. Use <see cref="ReadRandom"/> instead.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)] // Don't inline. This function serves as a pointer to override with cpu opcodes
    public static uint RdRandInternal(out byte carry)
    {
        // Needed to compile
        carry = byte.MaxValue;
        return 0;
    }
}
