using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ReadRandom;

internal static unsafe partial class CpuInstructions
{
    internal static readonly delegate* unmanaged<byte*, uint> RdSeed;
    internal static readonly delegate* unmanaged<byte*, uint> RdRand;

    private const uint MemCommit = 0x1000;
    private const uint MemReserve = 0x2000;
    private const uint MemRelease = 0x8000;
    private const uint PageReadWrite = 0x04;
    private const uint PageExecuteRead = 0x20;

    // The explicit static constructor initializes both pointers once, before any caller can use them.
    static CpuInstructions()
    {
        // Windows x64 passes the carry pointer in RCX and returns the value in EAX.
        ReadOnlySpan<byte> rdSeedOps = [
            0x0f, 0xc7, 0xf8, // rdseed eax
            0x0f, 0x92, 0x01, // setc byte ptr [rcx]
            0xc3              // ret
        ];
        ReadOnlySpan<byte> rdRandOps = [
            0x0f, 0xc7, 0xf0, // rdrand eax
            0x0f, 0x92, 0x01, // setc byte ptr [rcx]
            0xc3              // ret
        ];

        // Keep both entry points aligned to 16 bytes for indirect calls.
        const int rdRandOffset = 16;
        var size = (nuint)(rdRandOffset + rdRandOps.Length);
        var memory = VirtualAlloc(0, size, MemCommit | MemReserve, PageReadWrite);
        if (memory == 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        try
        {
            rdSeedOps.CopyTo(new Span<byte>((void*)memory, rdSeedOps.Length));
            rdRandOps.CopyTo(new Span<byte>((void*)(memory + rdRandOffset), rdRandOps.Length));

            if (VirtualProtect(memory, size, PageExecuteRead, out _) == 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            if (FlushInstructionCache(GetCurrentProcess(), memory, size) == 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            // Keep this single native allocation for the process lifetime. It is not GC-owned;
            // Windows reclaims it at process exit, so callers can always use these pointers.
            RdSeed = (delegate* unmanaged<byte*, uint>)memory;
            RdRand = (delegate* unmanaged<byte*, uint>)(memory + rdRandOffset);
        }
        catch
        {
            // No pointers have been published if initialization fails.
            _ = VirtualFree(memory, 0, MemRelease);
            throw;
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint VirtualAlloc(nint address, nuint size, uint allocationType, uint protection);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int VirtualProtect(nint address, nuint size, uint protection, out uint oldProtection);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int VirtualFree(nint address, nuint size, uint freeType);

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int FlushInstructionCache(nint process, nint address, nuint size);
}
