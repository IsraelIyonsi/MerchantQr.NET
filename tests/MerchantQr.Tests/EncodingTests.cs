using System.Text;
using MerchantQr;

namespace MerchantQr.Tests;

public class EncodingTests
{
    private const string MultibyteMerchantName = "東京カフェ"; // Tokyo cafe, all multibyte in UTF-8

    private static QrDataObject[] MultibyteFields() => new QrDataObject[]
    {
        new("00", "01"),
        new("53", "392"),
        new("58", "JP"),
        new("59", MultibyteMerchantName),
    };

    [Fact]
    public void Utf8Payload_Validates_WhenUtf8EncodingSupplied()
    {
        string payload = MerchantQrCode.Build(MultibyteFields(), Encoding.UTF8);

        QrPayload parsed = MerchantQrCode.Parse(payload, Encoding.UTF8);
        Assert.Equal(MultibyteMerchantName, parsed.MerchantName);
        Assert.Equal("JP", parsed.CountryCode);
    }

    [Fact]
    public void Utf8Payload_FailsCrc_UnderDefaultLatin1Encoding()
    {
        // A payload whose CRC was computed over UTF-8 bytes must not validate under the
        // Latin-1 low-byte default, proving the two models are distinct.
        string payload = MerchantQrCode.Build(MultibyteFields(), Encoding.UTF8);

        Assert.False(MerchantQrCode.TryParse(payload, out QrPayload? parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void AsciiPayload_Validates_UnderEitherEncoding()
    {
        // For ASCII fields the Latin-1 and UTF-8 byte models are identical, so a default-built
        // payload also validates when UTF-8 is supplied.
        string payload = MerchantQrCode.Build(new QrDataObject[]
        {
            new("00", "01"),
            new("53", "986"),
            new("58", "BR"),
            new("59", "ACME"),
        });

        Assert.True(MerchantQrCode.TryParse(payload, Encoding.UTF8, out QrPayload? parsed));
        Assert.NotNull(parsed);
    }

    [Fact]
    public void Crc_CheckValue_IsIdenticalUnderAsciiUtf8AndDefault()
    {
        Assert.Equal((ushort)0x29B1, Crc16Ccitt.Compute("123456789"));
        Assert.Equal((ushort)0x29B1, Crc16Ccitt.Compute("123456789", Encoding.UTF8));
        Assert.Equal("29B1", Crc16Ccitt.ComputeHex("123456789", Encoding.ASCII));
    }

    [Fact]
    public void Crc_MultibyteString_DiffersBetweenDefaultAndUtf8()
    {
        ushort latin1 = Crc16Ccitt.Compute(MultibyteMerchantName);
        ushort utf8 = Crc16Ccitt.Compute(MultibyteMerchantName, Encoding.UTF8);
        Assert.NotEqual(latin1, utf8);
    }
}
