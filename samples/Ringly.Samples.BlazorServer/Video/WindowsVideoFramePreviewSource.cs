using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Windows;

namespace Ringly.Samples.BlazorServer.Video;

public sealed class WindowsVideoFramePreviewSource : IVideoFramePreviewSource, IDisposable
{
    // Same 100ms-per-frame (10fps) throttle rationale as Ringly.Samples.BlazorHybrid's
    // VideoFramePreviewSource.cs — reassigning an <img> src faster than the browser can decode
    // the previous one queues up redundant work for no visible benefit.
    private static readonly TimeSpan MinImageUpdateInterval = TimeSpan.FromMilliseconds(100);

    private readonly WindowsVideoEndPoint videoEndPoint;
    private DateTime lastImageUpdateAt = DateTime.MinValue;

    public event Action<string>? RemoteFrameDataUriReady;

    public WindowsVideoFramePreviewSource(WindowsVideoEndPoint videoEndPoint)
    {
        this.videoEndPoint = videoEndPoint;
        this.videoEndPoint.OnVideoSinkDecodedSample += this.OnVideoSinkDecodedSample;
    }

    // Runs off the decode thread — the Core Component marshals to the UI thread itself when it
    // handles the view service's own StateChanged event, so this only needs to build the data URI
    // and raise the event.
    private void OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat)
    {
        // VP8Codec (this endpoint's encoder/decoder) always decodes to Bgr — matches
        // Ringly.Samples.Maui's CustomWindowsVideoEndPoint/AndroidVideoEndPoint, which rely on
        // the same underlying codec. Any other format is unexpected for this pipeline; skip
        // rather than risk building a corrupt bitmap from misinterpreted bytes.
        if (pixelFormat != VideoPixelFormatsEnum.Bgr)
        {
            return;
        }

        if (DateTime.UtcNow - this.lastImageUpdateAt < MinImageUpdateInterval)
        {
            return;
        }

        this.lastImageUpdateAt = DateTime.UtcNow;
        this.RemoteFrameDataUriReady?.Invoke(BuildBitmapDataUri((int)width, (int)height, stride, sample));
    }

    // Wraps a raw BGR24 buffer in a minimal 24bpp BMP container, base64-encoded as a data: URI an
    // <img> can render directly — the Blazor Server equivalent of
    // Ringly.Samples.BlazorHybrid's VideoFramePreviewSource/BuildBitmapDataUri. Stores rows
    // bottom-up (BMP's canonical layout); reads each source row using the decoder's own reported
    // stride rather than assuming width * 3, since WindowsVideoEndPoint reports it explicitly
    // (unlike CustomWindowsVideoEndPoint's DecodedFrameReady, which didn't).
    private static string BuildBitmapDataUri(int width, int height, int sourceStride, byte[] bgr)
    {
        int rowSize = ((width * 3) + 3) / 4 * 4;
        int pixelDataSize = rowSize * height;
        int fileSize = 54 + pixelDataSize;

        var bitmap = new byte[fileSize];
        bitmap[0] = (byte)'B';
        bitmap[1] = (byte)'M';
        BitConverter.GetBytes(fileSize).CopyTo(bitmap, 2);
        BitConverter.GetBytes(54).CopyTo(bitmap, 10);
        BitConverter.GetBytes(40).CopyTo(bitmap, 14);
        BitConverter.GetBytes(width).CopyTo(bitmap, 18);
        BitConverter.GetBytes(height).CopyTo(bitmap, 22);
        BitConverter.GetBytes((short)1).CopyTo(bitmap, 26);
        BitConverter.GetBytes((short)24).CopyTo(bitmap, 28);
        BitConverter.GetBytes(pixelDataSize).CopyTo(bitmap, 34);

        for (int row = 0; row < height; row++)
        {
            int sourceRowStart = row * sourceStride;
            int destinationRowStart = 54 + ((height - 1 - row) * rowSize);
            Array.Copy(bgr, sourceRowStart, bitmap, destinationRowStart, width * 3);
        }

        return $"data:image/bmp;base64,{Convert.ToBase64String(bitmap)}";
    }

    public void Dispose() => this.videoEndPoint.OnVideoSinkDecodedSample -= this.OnVideoSinkDecodedSample;
}
