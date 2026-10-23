using System;
using ReadRandom;
using Xunit;

namespace Tests;

public class CpuReaderTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(0x80000000u)]
    [InlineData(uint.MaxValue)]
    public void Read_ReturnsSuccessfulValueWithoutRetrying(uint expected)
    {
        var attempts = 0;
        uint Instruction(out byte carry)
        {
            Assert.Equal(1, ++attempts);
            carry = 1;
            return expected;
        }

        Assert.Equal(expected, CpuReader.Read(Instruction));
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(1, 0u)]
    [InlineData(1, uint.MaxValue)]
    [InlineData(3, 0u)]
    [InlineData(3, 42u)]
    public void Read_RetriesFailedAttemptsAndReturnsFirstSuccessfulValue(int failures, uint expected)
    {
        var attempts = 0;
        uint Instruction(out byte carry)
        {
            Assert.True(++attempts <= failures + 1, "Read continued after success.");
            carry = attempts <= failures ? (byte)0 : (byte)1;
            // A nonzero failed result must not be mistaken for success.
            return carry == 0 ? 123u : expected;
        }

        Assert.Equal(expected, CpuReader.Read(Instruction));
        Assert.Equal(failures + 1, attempts);
    }

    [Fact]
    public void Read_PropagatesInstructionExceptionAfterFailedAttempt()
    {
        var expected = new InvalidOperationException("Instruction failed.");
        var attempts = 0;
        uint Instruction(out byte carry)
        {
            carry = 0;
            if (++attempts == 1)
                return 0;
            throw expected;
        }

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => CpuReader.Read(Instruction)));
        Assert.Equal(2, attempts);
    }
}
