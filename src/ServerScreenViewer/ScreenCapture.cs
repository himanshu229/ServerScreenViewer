using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ServerScreenViewer;

internal static class ScreenCapture
{
    public static byte[] CaptureJpeg(AppConfig config)
    {
        var bounds = GetBounds(config.MonitorIndex);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("No interactive display is available in this Windows session.");

        using var source = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(source))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        var outputWidth = Math.Min(config.MaxWidth, source.Width);
        var outputHeight = (int)Math.Round(source.Height * (outputWidth / (double)source.Width));

        using var output = outputWidth == source.Width
            ? new Bitmap(source)
            : Resize(source, outputWidth, outputHeight);
        using var stream = new MemoryStream();

        var codec = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)config.JpegQuality);
        output.Save(stream, codec, parameters);
        return stream.ToArray();
    }

    private static Rectangle GetBounds(int monitorIndex)
    {
        var screens = Screen.AllScreens;
        if (monitorIndex >= 0)
        {
            if (monitorIndex >= screens.Length)
                throw new InvalidOperationException($"MonitorIndex {monitorIndex} is not available. Detected monitor count: {screens.Length}.");
            return screens[monitorIndex].Bounds;
        }

        return SystemInformation.VirtualScreen;
    }

    private static Bitmap Resize(Image source, int width, int height)
    {
        var target = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(target);
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.Bilinear;
        graphics.SmoothingMode = SmoothingMode.HighSpeed;
        graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
        graphics.DrawImage(source, 0, 0, width, height);
        return target;
    }
}
