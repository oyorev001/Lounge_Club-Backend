using System.Security.Cryptography;
using QRCoder;

namespace LoungeClubsApi.Helpers;

public static class QrCodeHelper
{
    public static string GenerateQrToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static byte[] GenerateQrCodePng(string data, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        return pngQrCode.GetGraphic(pixelsPerModule);
    }

    public static string GenerateQrCodeDataUri(string data, int pixelsPerModule = 10)
    {
        var bytes = GenerateQrCodePng(data, pixelsPerModule);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
