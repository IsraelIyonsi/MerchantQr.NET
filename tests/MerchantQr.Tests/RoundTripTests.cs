using MerchantQr;

namespace MerchantQr.Tests;

public class RoundTripTests
{
    private static readonly QrDataObject[] MerchantFields =
    {
        new("00", "01"),
        new("52", "5411"),
        new("53", "986"),
        new("54", "23.72"),
        new("58", "BR"),
        new("59", "BEST TRANSPORT"),
        new("60", "SAO PAULO"),
    };

    [Fact]
    public void Build_ThenParse_RecoversEveryField()
    {
        string payload = MerchantQrCode.Build(MerchantFields);
        QrPayload parsed = MerchantQrCode.Parse(payload);

        Assert.Equal("01", parsed.PayloadFormatIndicator);
        Assert.Equal("5411", parsed.MerchantCategoryCode);
        Assert.Equal("986", parsed.TransactionCurrency);
        Assert.Equal("23.72", parsed.TransactionAmount);
        Assert.Equal("BR", parsed.CountryCode);
        Assert.Equal("BEST TRANSPORT", parsed.MerchantName);
        Assert.Equal("SAO PAULO", parsed.MerchantCity);
    }

    [Fact]
    public void Build_ProducesValidCrc_ThatParseAccepts()
    {
        string payload = MerchantQrCode.Build(MerchantFields);

        Assert.True(MerchantQrCode.TryParse(payload, out QrPayload? parsed));
        Assert.NotNull(parsed);
        Assert.NotNull(parsed!.Crc);
        Assert.Equal(4, parsed.Crc!.Length);
    }

    [Fact]
    public void Build_AppendsCrcAsFinalObject()
    {
        string payload = MerchantQrCode.Build(MerchantFields);
        QrPayload parsed = MerchantQrCode.Parse(payload);

        Assert.Equal("63", parsed.Objects[^1].Id);
    }

    [Fact]
    public void Build_OverwritesAnyCallerSuppliedCrc()
    {
        var withBogusCrc = new List<QrDataObject>(MerchantFields)
        {
            new("63", "0000"),
        };

        string payload = MerchantQrCode.Build(withBogusCrc);
        QrPayload parsed = MerchantQrCode.Parse(payload);

        Assert.Single(parsed.Objects, o => o.Id == "63");
        Assert.NotEqual("0000", parsed.Crc);
    }

    [Fact]
    public void Get_ReturnsNull_ForAbsentId()
    {
        QrPayload parsed = MerchantQrCode.Parse(MerchantQrCode.Build(MerchantFields));
        Assert.Null(parsed.Get("99"));
    }

    [Fact]
    public void TransactionAmount_IsNull_ForStaticQr()
    {
        var staticFields = new QrDataObject[]
        {
            new("00", "01"),
            new("53", "986"),
            new("58", "BR"),
        };

        QrPayload parsed = MerchantQrCode.Parse(MerchantQrCode.Build(staticFields));
        Assert.Null(parsed.TransactionAmount);
    }
}
