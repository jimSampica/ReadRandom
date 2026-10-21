namespace Wetware.ReadRandom;

internal static class CpuReader
{
    internal delegate uint ReadInstruction(out byte carry);

    internal static uint Read(ReadInstruction readInstruction)
    {
        uint result;
        byte carry;
        // Carry indicates success; zero is a valid successful random value.
        do result = readInstruction(out carry);
        while (carry == 0);

        return result;
    }
}
