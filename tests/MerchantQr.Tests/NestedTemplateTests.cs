using MerchantQr;

namespace MerchantQr.Tests;

public class NestedTemplateTests
{
    [Fact]
    public void AdditionalDataTemplate_SubObject_RoundTrips()
    {
        var subObjects = new QrDataObject[]
        {
            new("01", "INV-2026-0007"),
            new("05", "REF12345"),
        };

        string additionalData = MerchantQrCode.Encode(subObjects);

        var objects = new QrDataObject[]
        {
            new("00", "01"),
            new("53", "986"),
            new("58", "BR"),
            new("62", additionalData),
        };

        string payload = MerchantQrCode.Build(objects);
        QrPayload parsed = MerchantQrCode.Parse(payload);

        IReadOnlyList<QrDataObject> subs = parsed.GetSubObjects("62");
        Assert.Equal(2, subs.Count);
        Assert.Equal("01", subs[0].Id);
        Assert.Equal("INV-2026-0007", subs[0].Value);
        Assert.Equal("05", subs[1].Id);
        Assert.Equal("REF12345", subs[1].Value);
    }

    [Fact]
    public void MerchantAccountTemplate_SubObject_RoundTrips()
    {
        var accountInfo = new QrDataObject[]
        {
            new("00", "BR.GOV.BCB.PIX"),
            new("01", "merchant@example.com"),
        };

        string template = MerchantQrCode.Encode(accountInfo);

        var objects = new QrDataObject[]
        {
            new("00", "01"),
            new("26", template),
            new("53", "986"),
            new("58", "BR"),
        };

        QrPayload parsed = MerchantQrCode.Parse(MerchantQrCode.Build(objects));

        IReadOnlyList<QrDataObject> subs = parsed.GetSubObjects("26");
        Assert.Equal("BR.GOV.BCB.PIX", subs[0].Value);
        Assert.Equal("merchant@example.com", subs[1].Value);
    }

    [Fact]
    public void NonTemplateField_HasNoSubObjects()
    {
        var objects = new QrDataObject[]
        {
            new("00", "01"),
            new("59", "ACME"),
            new("58", "BR"),
        };

        QrPayload parsed = MerchantQrCode.Parse(MerchantQrCode.Build(objects));
        Assert.Empty(parsed.GetSubObjects("59"));
    }
}
