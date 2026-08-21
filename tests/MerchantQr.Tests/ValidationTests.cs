using MerchantQr;

namespace MerchantQr.Tests;

public class ValidationTests
{
    private const int CrcHexLength = 4;

    private static string ValidPayload() => MerchantQrCode.Build(new QrDataObject[]
    {
        new("00", "01"),
        new("53", "986"),
        new("58", "BR"),
        new("59", "ACME"),
    });

    [Fact]
    public void Parse_FlippedCrcCharacter_Throws()
    {
        string payload = ValidPayload();
        char last = payload[^1];
        char replacement = last == '0' ? '1' : '0';
        string tampered = payload[..^1] + replacement;

        Assert.Throws<MerchantQrParseException>(() => MerchantQrCode.Parse(tampered));
    }

    [Fact]
    public void TryParse_FlippedCrcCharacter_ReturnsFalse()
    {
        string payload = ValidPayload();
        char last = payload[^1];
        char replacement = last == '0' ? '1' : '0';
        string tampered = payload[..^1] + replacement;

        Assert.False(MerchantQrCode.TryParse(tampered, out QrPayload? parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void Parse_LowercaseCrc_Validates()
    {
        // Build emits uppercase hex; Parse compares case-insensitively, so a lowercase CRC
        // (as some generators emit) still validates.
        string payload = ValidPayload();
        string lowered = payload[..^CrcHexLength] + payload[^CrcHexLength..].ToLowerInvariant();

        Assert.True(MerchantQrCode.TryParse(lowered, out QrPayload? parsed));
        Assert.NotNull(parsed);
    }

    [Fact]
    public void Parse_TruncatedValue_Throws()
    {
        // ID "00", declared length 10, but only 3 value characters remain.
        string malformed = "0010ABC";
        Assert.Throws<MerchantQrParseException>(() => MerchantQrCode.Parse(malformed));
    }

    [Fact]
    public void Parse_NonNumericLengthField_Throws()
    {
        string malformed = "00XX01" + "6304";
        Assert.Throws<MerchantQrParseException>(() => MerchantQrCode.Parse(malformed));
    }

    [Fact]
    public void Parse_MissingCrc_Throws()
    {
        string noCrc = "000201";
        Assert.Throws<MerchantQrParseException>(() => MerchantQrCode.Parse(noCrc));
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<MerchantQrParseException>(() => MerchantQrCode.Parse(null!));
    }

    [Fact]
    public void Build_OversizedValue_Throws()
    {
        var objects = new QrDataObject[] { new("59", new string('X', 100)) };
        Assert.Throws<ArgumentException>(() => MerchantQrCode.Build(objects));
    }

    [Fact]
    public void Build_BadIdLength_Throws()
    {
        var objects = new QrDataObject[] { new("5", "X") };
        Assert.Throws<ArgumentException>(() => MerchantQrCode.Build(objects));
    }
}
