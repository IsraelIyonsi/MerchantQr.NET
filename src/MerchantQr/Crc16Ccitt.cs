using System.Globalization;
using System.Text;
using MerchantQr.Internal;

namespace MerchantQr;

/// <summary>
/// CRC-16/CCITT-FALSE checksum: polynomial 0x1021, initial value 0xFFFF, no input
/// reflection, no output reflection, no final XOR. This is the checksum EMVCo mandates
/// for the CRC data object (ID "63") of a Merchant-Presented Mode QR payload.
/// </summary>
internal static class Crc16Ccitt
{
    /// <summary>
    /// Computes the CRC-16/CCITT-FALSE checksum over the bytes of <paramref name="input"/>.
    /// When <paramref name="encoding"/> is <see langword="null"/> (the default) each character
    /// is read as its Latin-1 low byte, which is exact for ASCII payloads; supply an
    /// <see cref="Encoding"/> such as <see cref="Encoding.UTF8"/> to checksum the encoded bytes
    /// of a payload carrying multibyte characters.
    /// </summary>
    /// <param name="input">The text to checksum.</param>
    /// <param name="encoding">The byte encoding, or <see langword="null"/> for the Latin-1 low-byte default.</param>
    /// <returns>The 16-bit checksum.</returns>
    public static ushort Compute(string input, Encoding? encoding = null)
    {
        ushort crc = CrcParameters.InitialValue;

        if (encoding is null)
        {
            foreach (char c in input)
            {
                crc = Update(crc, (byte)c);
            }
        }
        else
        {
            foreach (byte b in encoding.GetBytes(input))
            {
                crc = Update(crc, b);
            }
        }

        return crc;
    }

    /// <summary>
    /// Computes the checksum and formats it as four uppercase hexadecimal characters.
    /// </summary>
    /// <param name="input">The text to checksum.</param>
    /// <param name="encoding">The byte encoding, or <see langword="null"/> for the Latin-1 low-byte default.</param>
    /// <returns>The checksum as a 4-character uppercase hex string.</returns>
    public static string ComputeHex(string input, Encoding? encoding = null) =>
        Compute(input, encoding).ToString(CrcParameters.ValueFormat, CultureInfo.InvariantCulture);

    private static ushort Update(ushort crc, byte value)
    {
        crc ^= (ushort)(value << CrcParameters.ByteWidthBits);

        for (int bit = 0; bit < CrcParameters.BitsPerByte; bit++)
        {
            bool highBitSet = (crc & CrcParameters.HighBitMask) != 0;
            crc = (ushort)(crc << 1);
            if (highBitSet)
            {
                crc ^= CrcParameters.Polynomial;
            }
        }

        return crc;
    }
}
