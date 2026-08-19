namespace MerchantQr;

/// <summary>
/// Thrown when a Merchant-Presented Mode payload cannot be parsed: malformed TLV
/// structure (bad or truncated length), a missing CRC data object, or a CRC mismatch.
/// </summary>
public sealed class MerchantQrParseException : Exception
{
    /// <summary>Initializes a new instance with a descriptive message.</summary>
    /// <param name="message">The reason parsing failed.</param>
    public MerchantQrParseException(string message)
        : base(message)
    {
    }
}
