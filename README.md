# MerchantQr.Net

Correct, zero-dependency parser and generator for **EMVCo Merchant-Presented Mode (MPM)** QR code payloads: the TLV string behind Pix (Brazil), SGQR (Singapore), and many national merchant-QR schemes. CRC validated on parse, always recomputed on build. Deterministic, offline, Native AOT clean.

[![NuGet](https://img.shields.io/nuget/v/MerchantQr.Net.svg)](https://www.nuget.org/packages/MerchantQr.Net) &nbsp; MIT &nbsp; Zero dependencies &nbsp; Native AOT clean

## Why

Merchant QR is not one standard; it is one wire format wearing many national badges. EMVCo publishes the MPM specification, then each central bank profiles it: Brazil calls it Pix (BR Code), Singapore calls it SGQR, and India, Malaysia, Thailand and others each ship their own scheme on the same skeleton. The skeleton is always the same: a flat list of tag-length-value data objects ending in a CRC-16 checksum.

Almost every team that touches this reimplements the same two things and gets one of them wrong: the TLV framing, and the CRC. The CRC is the classic trap. It is **CRC-16/CCITT-FALSE** computed over the payload *including* its own `6304` header, and a single wrong parameter (an XOR here, a reflection there) produces a checksum that looks plausible and is rejected by every real acquirer. `MerchantQr.Net` is the small, correct primitive that gets both right, with the standard `123456789` check value pinned in the test suite.

## Install

```
dotnet add package MerchantQr.Net
```

## Parse

```csharp
using MerchantQr;

QrPayload qr = MerchantQrCode.Parse(payload); // throws MerchantQrParseException on a bad CRC or malformed TLV

string? version  = qr.PayloadFormatIndicator; // "01"
string? currency = qr.TransactionCurrency;    // ISO 4217 numeric, e.g. "986"
string? amount   = qr.TransactionAmount;      // raw string, null for a static QR
string? name     = qr.MerchantName;
string? city     = qr.MerchantCity;

// Non-throwing variant
if (MerchantQrCode.TryParse(payload, out QrPayload? parsed))
{
    // parsed.Objects is the ordered top-level TLV list
}
```

Nested templates (merchant account info `26`-`51`, additional data `62`, and the unreserved range) are parsed one level down:

```csharp
foreach (QrDataObject sub in qr.GetSubObjects("62"))
{
    // e.g. sub.Id == "05" (reference label)
}
```

## Build

`Build` serializes your data objects and appends a correct CRC, so its output always validates. Any CRC you pass in is discarded and recomputed.

```csharp
string payload = MerchantQrCode.Build(new[]
{
    new QrDataObject("00", "01"),            // Payload Format Indicator
    new QrDataObject("52", "5411"),          // Merchant Category Code
    new QrDataObject("53", "986"),           // Transaction Currency (BRL)
    new QrDataObject("54", "23.72"),         // Transaction Amount
    new QrDataObject("58", "BR"),            // Country Code
    new QrDataObject("59", "BEST TRANSPORT"),// Merchant Name
    new QrDataObject("60", "SAO PAULO"),     // Merchant City
});
```

For a nested template, encode the sub-objects first with `Encode` (a TLV fragment with no CRC), then hand that string to `Build`:

```csharp
string additionalData = MerchantQrCode.Encode(new[]
{
    new QrDataObject("05", "REF12345"),      // reference label
});

string payload = MerchantQrCode.Build(new[]
{
    new QrDataObject("00", "01"),
    new QrDataObject("62", additionalData),  // Additional Data template
});
```

## The CRC guarantee

The checksum is **CRC-16/CCITT-FALSE**: polynomial `0x1021`, initial value `0xFFFF`, no input reflection, no output reflection, no final XOR. It is computed over the entire payload *including* the CRC object's own `6304` header, up to but not including the four hex characters of the CRC value, over the Latin-1 bytes of the string.

The implementation is pinned to the industry check value: the CRC of the ASCII string `123456789` is `0x29B1`, asserted directly in the test suite. `Parse` rejects any payload whose CRC does not match; `Build` always emits a valid one.

## Format, in one paragraph

The payload is a flat concatenation of data objects. Each object is a 2-character ID, a 2-character zero-padded decimal length, then a value of exactly that length. `000201` means ID `00`, length `02`, value `01`. Reserved IDs include `00` (Payload Format Indicator), `52` (Merchant Category Code), `53` (Transaction Currency), `54` (Transaction Amount), `58` (Country Code), `59` (Merchant Name), `60` (Merchant City), `62` (Additional Data), and `63` (CRC). IDs `26`-`51`, `62`, `64` and `80`-`99` are nested templates whose value is itself a sequence of sub-objects.

## Schemes this covers

Any scheme that profiles EMVCo MPM shares this framing and CRC, so the parser and generator apply directly to Pix / BR Code (Brazil), SGQR (Singapore), and the EMVCo-based merchant-QR standards used across India, Malaysia, Thailand and other markets. Scheme-specific reserved-tag semantics (which merchant account template carries which identifier) sit on top of this primitive; this library gives you the correct wire format underneath.

## Notes and limitations

- The CRC is computed over Latin-1 bytes, matching payloads whose fields are ASCII (the overwhelmingly common case for Pix and SGQR). Payloads carrying multibyte UTF-8 in a field such as the Merchant Information Language template are outside this Latin-1 CRC model.
- Nested templates are parsed one level deep and exposed via `GetSubObjects`. A template value that is not well-formed sub-TLV is left opaque rather than rejected, so a payload with a valid top-level CRC still parses.
- Values are surfaced as raw strings. Currency and amount typing, and per-scheme field validation, are the caller's job.

## License

MIT. Copyright Israel Iyonsi.
