namespace MerchantQr.Internal;

internal static class TlvFormat
{
    public const int IdLength = 2;
    public const int LengthFieldLength = 2;
    public const int HeaderLength = IdLength + LengthFieldLength;
    public const int MaxValueLength = 99;
    public const int LengthFieldRadix = 10;
    public const string LengthFieldFormat = "D2";
}

internal static class FieldIds
{
    public const string PayloadFormatIndicator = "00";
    public const string PointOfInitiationMethod = "01";
    public const string MerchantCategoryCode = "52";
    public const string TransactionCurrency = "53";
    public const string TransactionAmount = "54";
    public const string CountryCode = "58";
    public const string MerchantName = "59";
    public const string MerchantCity = "60";
    public const string AdditionalData = "62";
    public const string Crc = "63";
}

internal static class NestedTemplates
{
    public const int MerchantAccountLow = 26;
    public const int MerchantAccountHigh = 51;
    public const int AdditionalData = 62;
    public const int MerchantInformationLanguage = 64;
    public const int UnreservedLow = 80;
    public const int UnreservedHigh = 99;

    public static bool IsNestedTemplate(string id)
    {
        if (!int.TryParse(id, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int n))
        {
            return false;
        }

        return (n >= MerchantAccountLow && n <= MerchantAccountHigh)
            || n == AdditionalData
            || n == MerchantInformationLanguage
            || (n >= UnreservedLow && n <= UnreservedHigh);
    }
}

internal static class CrcParameters
{
    public const ushort Polynomial = 0x1021;
    public const ushort InitialValue = 0xFFFF;
    public const int ValueLength = 4;
    public const string ValueFormat = "X4";
    public const int ByteWidthBits = 8;
    public const ushort HighBitMask = 0x8000;
    public const int BitsPerByte = 8;
}
