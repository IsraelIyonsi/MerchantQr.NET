using MerchantQr;

namespace MerchantQr.Tests;

public class DuplicateTemplateTests
{
    private const string TemplateId = "26";
    private const string FirstKey = "first@example.com";
    private const string SecondKey = "second@example.com";

    [Fact]
    public void DuplicateTemplateId_FirstWins_ForBothGetAndGetSubObjects()
    {
        string first = MerchantQrCode.Encode(new QrDataObject[]
        {
            new("00", "BR.GOV.BCB.PIX"),
            new("01", FirstKey),
        });

        string second = MerchantQrCode.Encode(new QrDataObject[]
        {
            new("00", "BR.GOV.BCB.PIX"),
            new("01", SecondKey),
        });

        string payload = MerchantQrCode.Build(new QrDataObject[]
        {
            new("00", "01"),
            new(TemplateId, first),
            new(TemplateId, second),
            new("53", "986"),
            new("58", "BR"),
        });

        QrPayload parsed = MerchantQrCode.Parse(payload);

        // Get already returns the first match; GetSubObjects must agree and expose the FIRST
        // template's sub-objects, not the last.
        Assert.Equal(first, parsed.Get(TemplateId));

        IReadOnlyList<QrDataObject> subs = parsed.GetSubObjects(TemplateId);
        Assert.Equal(FirstKey, subs[1].Value);
        Assert.NotEqual(SecondKey, subs[1].Value);
    }
}
