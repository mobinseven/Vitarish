using QRCoder;

namespace Vitarish.Main.Infrastructure.Links;

public sealed class QrCodePngGenerator : IQrCodePngGenerator
{
    public byte[] Generate(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        PngByteQRCode png = new(data);
        return png.GetGraphic(20);
    }
}
