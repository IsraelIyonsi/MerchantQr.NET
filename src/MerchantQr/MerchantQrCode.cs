using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using MerchantQr.Internal;

namespace MerchantQr;

/// <summary>
/// Parses and generates EMVCo Merchant-Presented Mode (MPM) QR code payloads: the flat
/// TLV string behind Pix, SGQR and many national merchant-QR schemes. Zero runtime
/// dependencies, deterministic, offline. The CRC (ID "63") is validated on parse and
/// always recomputed on build.
/// </summary>
public static class MerchantQrCode
{
    /// <summary>
    /// Parses a Merchant-Presented Mode payload, validating its TLV structure and CRC.
    /// </summary>
    /// <param name="payload">The raw QR payload string.</param>
    /// <param name="encoding">
    /// The byte encoding used to validate the CRC, or <see langword="null"/> (the default) for
    /// the Latin-1 low-byte model that is exact for ASCII payloads. Supply
    /// <see cref="Encoding.UTF8"/> to validate a payload whose fields carry multibyte characters.
    /// </param>
    /// <returns>The parsed payload.</returns>
    /// <exception cref="MerchantQrParseException">
    /// The payload is null, malformed (bad or truncated length), missing its CRC data
    /// object, or its CRC does not match the computed value.
    /// </exception>
    public static QrPayload Parse(string payload, Encoding? encoding = null)
    {
        if (payload is null)
        {
            throw new MerchantQrParseException("Payload is null.");
        }

        IReadOnlyList<QrDataObject> objects = ParseObjects(payload);
        ValidateCrc(payload, objects, encoding);
        return new QrPayload(objects, BuildSubObjectMap(objects));
    }

    /// <summary>
    /// Attempts to parse a Merchant-Presented Mode payload. Never throws.
    /// </summary>
    /// <param name="payload">The raw QR payload string.</param>
    /// <param name="result">The parsed payload on success; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string payload, [NotNullWhen(true)] out QrPayload? result) =>
        TryParse(payload, null, out result);

    /// <summary>
    /// Attempts to parse a Merchant-Presented Mode payload with an explicit CRC encoding.
    /// Never throws.
    /// </summary>
    /// <param name="payload">The raw QR payload string.</param>
    /// <param name="encoding">
    /// The byte encoding used to validate the CRC, or <see langword="null"/> for the Latin-1
    /// low-byte default.
    /// </param>
    /// <param name="result">The parsed payload on success; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string payload, Encoding? encoding, [NotNullWhen(true)] out QrPayload? result)
    {
        try
        {
            result = Parse(payload, encoding);
            return true;
        }
        catch (MerchantQrParseException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes data objects into a complete payload, appending a freshly computed CRC
    /// (ID "63") so the output always carries a valid checksum. Any CRC object supplied by
    /// the caller is discarded and replaced.
    /// </summary>
    /// <param name="objects">The data objects, in the desired order, excluding the CRC.</param>
    /// <param name="encoding">
    /// The byte encoding used to compute the CRC, or <see langword="null"/> (the default) for
    /// the Latin-1 low-byte model that is exact for ASCII payloads. Supply the same encoding to
    /// <see cref="Parse(string, Encoding?)"/> to validate the result.
    /// </param>
    /// <returns>The complete payload string with a valid CRC.</returns>
    /// <exception cref="ArgumentException">A data object is malformed (bad id or oversized value).</exception>
    public static string Build(IEnumerable<QrDataObject> objects, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(objects);

        var body = new StringBuilder();
        foreach (QrDataObject o in objects)
        {
            if (string.Equals(o.Id, FieldIds.Crc, StringComparison.Ordinal))
            {
                continue;
            }

            AppendObject(body, o);
        }

        body.Append(FieldIds.Crc);
        body.Append(CrcParameters.ValueLength.ToString(TlvFormat.LengthFieldFormat, CultureInfo.InvariantCulture));
        body.Append(Crc16Ccitt.ComputeHex(body.ToString(), encoding));
        return body.ToString();
    }

    /// <summary>
    /// Serializes data objects into a TLV fragment without a CRC. Use this to produce the
    /// value of a nested template (for example the sub-objects of an Additional Data
    /// template, ID "62") before passing that template to <see cref="Build"/>.
    /// </summary>
    /// <param name="objects">The data objects to encode.</param>
    /// <returns>The concatenated TLV fragment.</returns>
    /// <exception cref="ArgumentException">A data object is malformed (bad id or oversized value).</exception>
    public static string Encode(IEnumerable<QrDataObject> objects)
    {
        ArgumentNullException.ThrowIfNull(objects);

        var body = new StringBuilder();
        foreach (QrDataObject o in objects)
        {
            AppendObject(body, o);
        }

        return body.ToString();
    }

    private static void AppendObject(StringBuilder body, QrDataObject o)
    {
        if (o.Id is null || o.Id.Length != TlvFormat.IdLength)
        {
            throw new ArgumentException($"Data object id must be exactly {TlvFormat.IdLength} characters.", nameof(o));
        }

        string value = o.Value ?? string.Empty;
        if (value.Length > TlvFormat.MaxValueLength)
        {
            throw new ArgumentException(
                $"Data object '{o.Id}' value length {value.Length} exceeds the maximum of {TlvFormat.MaxValueLength}.",
                nameof(o));
        }

        body.Append(o.Id);
        body.Append(value.Length.ToString(TlvFormat.LengthFieldFormat, CultureInfo.InvariantCulture));
        body.Append(value);
    }

    private static IReadOnlyList<QrDataObject> ParseObjects(string payload)
    {
        var objects = new List<QrDataObject>();
        int index = 0;

        while (index < payload.Length)
        {
            if (index + TlvFormat.HeaderLength > payload.Length)
            {
                throw new MerchantQrParseException(
                    $"Truncated data object at position {index}: not enough characters for id and length.");
            }

            string id = payload.Substring(index, TlvFormat.IdLength);
            string lengthField = payload.Substring(index + TlvFormat.IdLength, TlvFormat.LengthFieldLength);

            if (!int.TryParse(lengthField, NumberStyles.None, CultureInfo.InvariantCulture, out int length))
            {
                throw new MerchantQrParseException(
                    $"Data object '{id}' has a non-numeric length field '{lengthField}'.");
            }

            int valueStart = index + TlvFormat.HeaderLength;
            if (valueStart + length > payload.Length)
            {
                throw new MerchantQrParseException(
                    $"Data object '{id}' declares length {length} but the payload ends early.");
            }

            objects.Add(new QrDataObject(id, payload.Substring(valueStart, length)));
            index = valueStart + length;
        }

        return objects;
    }

    private static void ValidateCrc(string payload, IReadOnlyList<QrDataObject> objects, Encoding? encoding)
    {
        if (objects.Count == 0)
        {
            throw new MerchantQrParseException("Payload contains no data objects and no CRC.");
        }

        QrDataObject crcObject = objects[objects.Count - 1];
        if (!string.Equals(crcObject.Id, FieldIds.Crc, StringComparison.Ordinal))
        {
            throw new MerchantQrParseException(
                $"The final data object must be the CRC (ID '{FieldIds.Crc}') but was '{crcObject.Id}'.");
        }

        if (crcObject.Value.Length != CrcParameters.ValueLength)
        {
            throw new MerchantQrParseException(
                $"CRC value must be exactly {CrcParameters.ValueLength} characters.");
        }

        int checksumStart = payload.Length - CrcParameters.ValueLength;
        string covered = payload.Substring(0, checksumStart);
        string expected = Crc16Ccitt.ComputeHex(covered, encoding);

        if (!string.Equals(expected, crcObject.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new MerchantQrParseException(
                $"CRC mismatch: computed '{expected}' but payload declares '{crcObject.Value}'.");
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<QrDataObject>> BuildSubObjectMap(
        IReadOnlyList<QrDataObject> objects)
    {
        var map = new Dictionary<string, IReadOnlyList<QrDataObject>>(StringComparer.Ordinal);

        foreach (QrDataObject o in objects)
        {
            if (!NestedTemplates.IsNestedTemplate(o.Id) || map.ContainsKey(o.Id))
            {
                continue;
            }

            TryParseSubObjects(o.Value, out IReadOnlyList<QrDataObject> subs);
            map[o.Id] = subs;
        }

        return map;
    }

    private static bool TryParseSubObjects(string value, out IReadOnlyList<QrDataObject> subs)
    {
        try
        {
            subs = ParseObjects(value);
            return true;
        }
        catch (MerchantQrParseException)
        {
            subs = Array.Empty<QrDataObject>();
            return false;
        }
    }
}
