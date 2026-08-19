using MerchantQr.Internal;

namespace MerchantQr;

/// <summary>
/// A parsed EMVCo Merchant-Presented Mode payload: the ordered top-level data objects,
/// their parsed sub-objects for nested templates, and typed conveniences for the common fields.
/// </summary>
public sealed class QrPayload
{
    private static readonly IReadOnlyList<QrDataObject> Empty = Array.Empty<QrDataObject>();

    private readonly IReadOnlyDictionary<string, IReadOnlyList<QrDataObject>> _subObjects;

    internal QrPayload(
        IReadOnlyList<QrDataObject> objects,
        IReadOnlyDictionary<string, IReadOnlyList<QrDataObject>> subObjects)
    {
        Objects = objects;
        _subObjects = subObjects;
    }

    /// <summary>The top-level data objects in the order they appear in the payload.</summary>
    public IReadOnlyList<QrDataObject> Objects { get; }

    /// <summary>
    /// Returns the value of the first top-level data object with the given
    /// <paramref name="id"/>, or <see langword="null"/> when no such object exists.
    /// </summary>
    /// <param name="id">The two-character data object identifier.</param>
    /// <returns>The matching value, or <see langword="null"/>.</returns>
    public string? Get(string id)
    {
        foreach (QrDataObject o in Objects)
        {
            if (string.Equals(o.Id, id, StringComparison.Ordinal))
            {
                return o.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the parsed sub-objects of the nested template with the given
    /// <paramref name="id"/>, or an empty list when the object is absent or its value
    /// is not a well-formed sub-TLV sequence.
    /// </summary>
    /// <param name="id">The two-character template identifier, for example "62".</param>
    /// <returns>The parsed sub-objects, or an empty list.</returns>
    public IReadOnlyList<QrDataObject> GetSubObjects(string id) =>
        _subObjects.TryGetValue(id, out IReadOnlyList<QrDataObject>? subs) ? subs : Empty;

    /// <summary>Payload Format Indicator (ID "00"), "01" for the current EMVCo version.</summary>
    public string? PayloadFormatIndicator => Get(FieldIds.PayloadFormatIndicator);

    /// <summary>Point of Initiation Method (ID "01"), "11" static or "12" dynamic.</summary>
    public string? PointOfInitiationMethod => Get(FieldIds.PointOfInitiationMethod);

    /// <summary>Merchant Category Code (ID "52"), an ISO 18245 four-digit code.</summary>
    public string? MerchantCategoryCode => Get(FieldIds.MerchantCategoryCode);

    /// <summary>Transaction Currency (ID "53"), an ISO 4217 three-digit numeric code.</summary>
    public string? TransactionCurrency => Get(FieldIds.TransactionCurrency);

    /// <summary>Transaction Amount (ID "54") as its raw string, absent for static QR codes.</summary>
    public string? TransactionAmount => Get(FieldIds.TransactionAmount);

    /// <summary>Country Code (ID "58"), an ISO 3166-1 alpha-2 code.</summary>
    public string? CountryCode => Get(FieldIds.CountryCode);

    /// <summary>Merchant Name (ID "59").</summary>
    public string? MerchantName => Get(FieldIds.MerchantName);

    /// <summary>Merchant City (ID "60").</summary>
    public string? MerchantCity => Get(FieldIds.MerchantCity);

    /// <summary>The four-character uppercase hex CRC (ID "63") as it appears in the payload.</summary>
    public string? Crc => Get(FieldIds.Crc);
}
