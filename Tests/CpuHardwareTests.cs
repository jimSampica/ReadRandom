using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Xunit;

namespace Tests;

public class CpuHardwareTests
{
    [CpuHardwareFact(seed: false)]
    [Trait("Category", "Hardware")]
    public Task ReadRandom_AndRdRandInternal_CompleteSuccessfully() => RunProbe("rdrand");

    [CpuHardwareFact(seed: true)]
    [Trait("Category", "Hardware")]
    public Task ReadRandomSeed_AndRdSeedInternal_CompleteSuccessfully() => RunProbe("rdseed");

    [Fact]
    public Task Reads_WithHardwareIntrinsicsDisabled_ThrowPlatformNotSupported() => RunProbe("unsupported", disableHardwareIntrinsics: true);

    private static async Task RunProbe(string instruction, bool disableHardwareIntrinsics = false)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(typeof(CpuHardwareTests).Assembly.Location);
        startInfo.ArgumentList.Add("--hardware-probe");
        startInfo.ArgumentList.Add(instruction);
        // Verify the normal runtime configuration even if the parent disabled W^X.
        startInfo.Environment.Remove("DOTNET_EnableWriteXorExecute");
        startInfo.Environment.Remove("COMPlus_EnableWriteXorExecute");
        if (disableHardwareIntrinsics)
        {
            startInfo.Environment["DOTNET_EnableHWIntrinsic"] = "0";
            startInfo.Environment["COMPlus_EnableHWIntrinsic"] = "0";
        }

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        var exit = process.WaitForExitAsync();
        if (await Task.WhenAny(exit, Task.Delay(TimeSpan.FromSeconds(15))) != exit)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail($"{instruction} did not complete within 15 seconds.");
        }

        await exit;
        var output = await stdout;
        var error = await stderr;
        Assert.True(process.ExitCode == 0,
            $"{instruction} probe exited with code {process.ExitCode}.{Environment.NewLine}{output}{error}");
        Assert.Contains("Hardware probe passed.", output);
    }
}

public sealed class CpuHardwareFactAttribute : FactAttribute
{
    public CpuHardwareFactAttribute(bool seed)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            Skip = "The current opcode implementation requires Windows x64.";
            return;
        }

        if (!X86Base.IsSupported)
        {
            Skip = "CPUID is unavailable.";
            return;
        }

        var maxLeaf = X86Base.CpuId(0, 0).Eax;
        var supported = seed
            ? maxLeaf >= 7 && (X86Base.CpuId(7, 0).Ebx & (1 << 18)) != 0
            : maxLeaf >= 1 && (X86Base.CpuId(1, 0).Ecx & (1 << 30)) != 0;
        if (!supported)
            Skip = seed ? "CPU does not support RDSEED." : "CPU does not support RDRAND.";
    }
}
