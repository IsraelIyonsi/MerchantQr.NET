using System.Globalization;
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
    /// Computes the CRC-16/CCITT-FALSE checksum over the Latin-1 bytes of <paramref name="input"/>.
    /// </summary>
    /// <param name="input">The text to checksum. Characters are read as Latin-1 (low byte).</param>
    /// <returns>The 16-bit checksum.</returns>
    public static ushort Compute(string input)
    {
        ushort crc = CrcParameters.InitialValue;

        foreach (char c in input)
        {
            crc ^= (ushort)((byte)c << CrcParameters.ByteWidthBits);

            for (int bit = 0; bit < CrcParameters.BitsPerByte; bit++)
            {
                bool highBitSet = (crc & CrcParameters.HighBitMask) != 0;
                crc = (ushort)(crc << 1);
                if (highBitSet)
                {
                    crc ^= CrcParameters.Polynomial;
                }
            }
        }

        return crc;
    }

    /// <summary>
    /// Computes the checksum and formats it as four uppercase hexadecimal characters.
    /// </summary>
    /// <param name="input">The text to checksum.</param>
    /// <returns>The checksum as a 4-character uppercase hex string.</returns>
    public static string ComputeHex(string input) =>
        Compute(input).ToString(CrcParameters.ValueFormat, CultureInfo.InvariantCulture);
}
