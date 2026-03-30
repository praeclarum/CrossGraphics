using System.Runtime.InteropServices;

namespace CrossGraphicsTests;

static class Compare
{
    public static bool FilesMatch(string pendingPath, string acceptedPath, int tolerance = 0)
    {
        if (!File.Exists(acceptedPath))
            return false;

        if (pendingPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return SvgFilesMatch(pendingPath, acceptedPath);

        if (pendingPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return PngFilesMatch(pendingPath, acceptedPath, tolerance);

        return false;
    }

    static bool SvgFilesMatch(string pendingPath, string acceptedPath)
    {
        var pending = File.ReadAllText(pendingPath);
        var accepted = File.ReadAllText(acceptedPath);
        return string.Equals(pending, accepted, StringComparison.Ordinal);
    }

    #if __MACOS__ || __IOS__ || __MACCATALYST__
    static bool PngFilesMatch(string pendingPath, string acceptedPath, int tolerance)
    {
        var pendingUrl = Foundation.NSUrl.FromFilename(pendingPath);
        var acceptedUrl = Foundation.NSUrl.FromFilename(acceptedPath);
        if (pendingUrl is null || acceptedUrl is null)
            return false;

        using var pendingSource = ImageIO.CGImageSource.FromUrl(pendingUrl);
        using var acceptedSource = ImageIO.CGImageSource.FromUrl(acceptedUrl);
        if (pendingSource is null || acceptedSource is null)
            return false;

        var imageOptions = new ImageIO.CGImageOptions {
            ShouldCache = false,
            ShouldAllowFloat = true,
        };
        using var pendingImage = pendingSource.CreateImage(0, imageOptions);
        using var acceptedImage = acceptedSource.CreateImage(0, imageOptions);
        if (pendingImage is null || acceptedImage is null)
            return false;

        var width = (int)pendingImage.Width;
        var height = (int)pendingImage.Height;
        if (width != (int)acceptedImage.Width || height != (int)acceptedImage.Height)
            return false;

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

        if (tolerance <= 0)
            return pendingBytes.AsSpan().SequenceEqual(acceptedBytes);
        for (int i = 0; i < totalBytes; i++) {
            if (Math.Abs(pendingBytes[i] - acceptedBytes[i]) > tolerance)
                return false;
        }
        return true;
    }
    #else
    static bool PngFilesMatch(string pendingPath, string acceptedPath, int tolerance)
    {
        var pending = File.ReadAllBytes(pendingPath);
        var accepted = File.ReadAllBytes(acceptedPath);
        return pending.AsSpan().SequenceEqual(accepted);
    }
    #endif
}
