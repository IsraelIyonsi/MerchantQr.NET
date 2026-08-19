namespace MerchantQr;

/// <summary>
/// A single EMVCo TLV data object: a two-character <paramref name="Id"/> and its
/// <paramref name="Value"/>. The length field is derived from the value and is not stored.
/// </summary>
/// <param name="Id">The two-character data object identifier, for example "00" or "53".</param>
/// <param name="Value">The raw value. For nested templates this is the concatenated sub-TLV string.</param>
public readonly record struct QrDataObject(string Id, string Value);
