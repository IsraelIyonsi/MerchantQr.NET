using MerchantQr;

namespace MerchantQr.Tests;

public class InteropVectorTests
{
    private const string PixGui = "BR.GOV.BCB.PIX";
    private const string PixKey = "fulano@example.com";
    private const string ReferenceLabel = "***";
    private const string MerchantAccountId = "26";
    private const string AdditionalDataId = "62";
    private const char MaxAsciiChar = (char)0x7F;

    // A realistic Pix (BR Code) static merchant QR: nested ID-26 merchant account with the
    // BR.GOV.BCB.PIX GUI and a pix key, plus the standard EMVCo reserved fields and a nested
    // ID-62 additional-data template. Built with self-validating Build so the CRC is correct
    // over the ASCII bytes; every field must survive a Parse round-trip.
    private static string PixStaticPayload()
    {
        string merchantAccount = MerchantQrCode.Encode(new QrDataObject[]
        {
            new("00", PixGui),
            new("01", PixKey),
        });

        string additionalData = MerchantQrCode.Encode(new QrDataObject[]
        {
            new("05", ReferenceLabel),
        });

        return MerchantQrCode.Build(new QrDataObject[]
        {
            new("00", "01"),
            new("01", "11"),
            new(MerchantAccountId, merchantAccount),
            new("52", "0000"),
            new("53", "986"),
            new("58", "BR"),
            new("59", "FULANO DE TAL"),
            new("60", "BRASILIA"),
            new(AdditionalDataId, additionalData),
        });
    }

    [Fact]
    public void PixStatic_IsAsciiOnly_SoLatin1CrcIsExact()
    {
        string payload = PixStaticPayload();
        Assert.All(payload, c => Assert.True(c <= MaxAsciiChar));
    }

    [Fact]
    public void PixStatic_Parse_ValidatesCrc()
    {
        // Parse validates the CRC; a successful parse over the ASCII payload proves the
        // checksum is correct for the Latin-1/ASCII byte model.
        string payload = PixStaticPayload();
        Assert.True(MerchantQrCode.TryParse(payload, out QrPayload? parsed));
        Assert.NotNull(parsed);
        Assert.NotNull(parsed!.Crc);
        Assert.Equal(4, parsed.Crc!.Length);
    }

    [Fact]
    public void PixStatic_ExposesExpectedTopLevelFields()
    {
        QrPayload parsed = MerchantQrCode.Parse(PixStaticPayload());

        Assert.Equal("01", parsed.PayloadFormatIndicator);
        Assert.Equal("11", parsed.PointOfInitiationMethod);
        Assert.Equal("0000", parsed.MerchantCategoryCode);
        Assert.Equal("986", parsed.TransactionCurrency);
        Assert.Equal("BR", parsed.CountryCode);
        Assert.Equal("FULANO DE TAL", parsed.MerchantName);
        Assert.Equal("BRASILIA", parsed.MerchantCity);
    }

    [Fact]
    public void PixStatic_ExposesNestedMerchantAccount()
    {
        QrPayload parsed = MerchantQrCode.Parse(PixStaticPayload());

        IReadOnlyList<QrDataObject> account = parsed.GetSubObjects(MerchantAccountId);
        Assert.Equal("00", account[0].Id);
        Assert.Equal(PixGui, account[0].Value);
        Assert.Equal("01", account[1].Id);
        Assert.Equal(PixKey, account[1].Value);

        IReadOnlyList<QrDataObject> additional = parsed.GetSubObjects(AdditionalDataId);
        Assert.Equal("05", additional[0].Id);
        Assert.Equal(ReferenceLabel, additional[0].Value);
    }
}
