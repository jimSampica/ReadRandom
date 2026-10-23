using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Wetware.ReadRandom;

/// <summary>Contains methods used for reading random numbers generated from the CPU's digital random number generator (DRNG).</summary>
public static class Cpu
{
    /// <summary>Whether the current platform and CPU support reading random seeds using RDSEED.</summary>
    public static bool IsRdSeedSupported { get; } = IsSupported(seed: true);

    /// <summary>Whether the current platform and CPU support reading random values using RDRAND.</summary>
    public static bool IsRdRandSupported { get; } = IsSupported(seed: false);

    /// <summary>Reads a random seed value from the CPU's enhanced, nondeterministic random number generator (ENRNG).</summary>
    /// <exception cref="PlatformNotSupportedException">Windows x64 and RDSEED support are required.</exception>
    public static uint ReadRandomSeed() => CpuReader.Read(RdSeedInternal);

    /// <summary>Reads a random value from the CPU's deterministic random bit generator (DRBG).</summary>
    /// <exception cref="PlatformNotSupportedException">Windows x64 and RDRAND support are required.</exception>
    public static uint ReadRandom() => CpuReader.Read(RdRandInternal);

    /// <summary>Attempts to read a random seed by executing RDSEED once.</summary>
    /// <param name="carry">1 if a seed is available; otherwise, 0.</param>
    /// <returns>The seed on success, or zero on failure. Zero is also a valid successful seed.</returns>
    /// <exception cref="PlatformNotSupportedException">Windows x64 and RDSEED support are required.</exception>
    internal static unsafe uint RdSeedInternal(out byte carry)
    {
        if (!IsRdSeedSupported)
            throw new PlatformNotSupportedException("RDSEED requires Windows x64 and a CPU that supports RDSEED.");

        byte status;
        var result = CpuInstructions.RdSeed(&status);
        carry = status;
        return result;
    }

    /// <summary>Attempts to read a random value by executing RDRAND once.</summary>
    /// <param name="carry">1 if a value is available; otherwise, 0.</param>
    /// <returns>The value on success, or zero on failure. Zero is also a valid successful value.</returns>
    /// <exception cref="PlatformNotSupportedException">Windows x64 and RDRAND support are required.</exception>
    internal static unsafe uint RdRandInternal(out byte carry)
    {
        if (!IsRdRandSupported)
            throw new PlatformNotSupportedException("RDRAND requires Windows x64 and a CPU that supports RDRAND.");

        byte status;
        var result = CpuInstructions.RdRand(&status);
        carry = status;
        return result;
    }

    private static bool IsSupported(bool seed)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64 || !X86Base.IsSupported)
            return false;

        var maxLeaf = X86Base.CpuId(0, 0).Eax;
        return seed
            ? maxLeaf >= 7 && (X86Base.CpuId(7, 0).Ebx & (1 << 18)) != 0
            : maxLeaf >= 1 && (X86Base.CpuId(1, 0).Ecx & (1 << 30)) != 0;
    }
}
