using MerchantQr;

namespace MerchantQr.Tests;

public class Crc16CcittTests
{
    [Fact]
    public void Compute_CheckString_MatchesStandardCheckValue()
    {
        // The universal check value for CRC-16/CCITT-FALSE: CRC of ASCII "123456789" is 0x29B1.
        // This pins the implementation to the standard algorithm.
        Assert.Equal((ushort)0x29B1, Crc16Ccitt.Compute("123456789"));
    }

    [Fact]
    public void ComputeHex_CheckString_IsUppercaseFourHex()
    {
        Assert.Equal("29B1", Crc16Ccitt.ComputeHex("123456789"));
    }

    [Fact]
    public void Compute_EmptyString_IsInitialValue()
    {
        Assert.Equal((ushort)0xFFFF, Crc16Ccitt.Compute(string.Empty));
    }

    [Fact]
    public void ComputeHex_AlwaysFourCharacters()
    {
        Assert.Equal(4, Crc16Ccitt.ComputeHex("00020101").Length);
    }
}
