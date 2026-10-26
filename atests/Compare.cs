using System.Runtime.InteropServices;

namespace CrossGraphicsTests;

static class Compare
{
    public static bool FilesMatch(string pendingPath, string acceptedPath, int tolerance, out string? mismatch)
    {
        mismatch = GetMismatch(pendingPath, acceptedPath, tolerance);
        return mismatch is null;
    }

    static string? GetMismatch(string pendingPath, string acceptedPath, int tolerance)
    {
        if (!File.Exists(acceptedPath))
            return "No accepted image";

        if (pendingPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return SvgMismatch(pendingPath, acceptedPath);

        if (pendingPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return PngMismatch(pendingPath, acceptedPath, tolerance);

        return "Unsupported file type";
    }

    static string? SvgMismatch(string pendingPath, string acceptedPath)
    {
        var pending = File.ReadAllText(pendingPath);
        var accepted = File.ReadAllText(acceptedPath);
        return string.Equals(pending, accepted, StringComparison.Ordinal) ? null : "SVG text differs";
    }

    #if __MACOS__ || __IOS__ || __MACCATALYST__
    static string? PngMismatch(string pendingPath, string acceptedPath, int tolerance)
    {
        var pendingUrl = Foundation.NSUrl.FromFilename(pendingPath);
        var acceptedUrl = Foundation.NSUrl.FromFilename(acceptedPath);
        if (pendingUrl is null || acceptedUrl is null)
            return "Could not open image";

        using var pendingSource = ImageIO.CGImageSource.FromUrl(pendingUrl);
        using var acceptedSource = ImageIO.CGImageSource.FromUrl(acceptedUrl);
        if (pendingSource is null || acceptedSource is null)
            return "Could not read image";

        var imageOptions = new ImageIO.CGImageOptions {
            ShouldCache = false,
            ShouldAllowFloat = true,
        };
        using var pendingImage = pendingSource.CreateImage(0, imageOptions);
        using var acceptedImage = acceptedSource.CreateImage(0, imageOptions);
        if (pendingImage is null || acceptedImage is null)
            return "Could not decode image";

        var width = (int)pendingImage.Width;
        var height = (int)pendingImage.Height;
        if (width != (int)acceptedImage.Width || height != (int)acceptedImage.Height)
            return $"Size {width}x{height}, accepted {acceptedImage.Width}x{acceptedImage.Height}";

        var bytesPerRow = width * 4;
        var totalBytes = bytesPerRow * height;
        using var cs = CoreGraphics.CGColorSpace.CreateDeviceRGB();

        using var pendingCtx = new CoreGraphics.CGBitmapContext(
            null, width, height, 8, bytesPerRow, cs,
            CoreGraphics.CGBitmapFlags.PremultipliedLast);
        using var acceptedCtx = new CoreGraphics.CGBitmapContext(
            null, width, height, 8, bytesPerRow, cs,
            CoreGraphics.CGBitmapFlags.PremultipliedLast);

        var rect = new CoreGraphics.CGRect(0, 0, width, height);
        pendingCtx.DrawImage(rect, pendingImage);
        acceptedCtx.DrawImage(rect, acceptedImage);

        var pendingBytes = new byte[totalBytes];
        var acceptedBytes = new byte[totalBytes];
        Marshal.Copy(pendingCtx.Data, pendingBytes, 0, totalBytes);
        Marshal.Copy(acceptedCtx.Data, acceptedBytes, 0, totalBytes);

        var differingPixels = 0;
        var maxError = 0;
        for (int i = 0; i < totalBytes; i += 4) {
            var pixelError = 0;
            for (int c = 0; c < 4; c++)
                pixelError = Math.Max(pixelError, Math.Abs(pendingBytes[i + c] - acceptedBytes[i + c]));
            if (pixelError > tolerance)
                differingPixels++;
            maxError = Math.Max(maxError, pixelError);
        }
        if (differingPixels == 0)
            return null;
        var tolText = tolerance > 0 ? $" (tolerance {tolerance})" : "";
        return $"{differingPixels} px differ, max error {maxError}{tolText}";
    }
    #else
    static string? PngMismatch(string pendingPath, string acceptedPath, int tolerance)
    {
        var pending = File.ReadAllBytes(pendingPath);
        var accepted = File.ReadAllBytes(acceptedPath);
        return pending.AsSpan().SequenceEqual(accepted) ? null : "PNG bytes differ";
    }
    #endif
}
