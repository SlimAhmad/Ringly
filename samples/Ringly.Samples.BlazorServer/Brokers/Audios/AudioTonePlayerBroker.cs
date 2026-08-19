using NAudio.Wave;

namespace Ringly.Samples.BlazorServer.Brokers.Audios;

public sealed class AudioTonePlayerBroker : IAudioTonePlayerBroker, IDisposable
{
    private WaveOutEvent? waveOut;
    private WaveFileReader? waveReader;

    public ValueTask PlayLoopedAsync(Stream toneWav)
    {
        this.StopPlayback();

        this.waveReader = new WaveFileReader(toneWav);
        var loopStream = new LoopStream(this.waveReader);
        this.waveOut = new WaveOutEvent();
        this.waveOut.Init(loopStream);
        this.waveOut.Play();

        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync()
    {
        this.StopPlayback();
        return ValueTask.CompletedTask;
    }

    private void StopPlayback()
    {
        this.waveOut?.Stop();
        this.waveOut?.Dispose();
        this.waveOut = null;

        this.waveReader?.Dispose();
        this.waveReader = null;
    }

    public void Dispose() => this.StopPlayback();

    // NAudio idiom: wraps a seekable WaveStream so Read() loops back to the start indefinitely
    // instead of stopping once the underlying stream reaches its end — WaveOutEvent has no
    // built-in "loop" concept of its own.
    private sealed class LoopStream : WaveStream
    {
        private readonly WaveStream sourceStream;

        public LoopStream(WaveStream sourceStream) => this.sourceStream = sourceStream;

        public override WaveFormat WaveFormat => this.sourceStream.WaveFormat;

        public override long Length => this.sourceStream.Length;

        public override long Position
        {
            get => this.sourceStream.Position;
            set => this.sourceStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = this.sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                {
                    if (this.sourceStream.Position == 0)
                    {
                        break;
                    }

                    this.sourceStream.Position = 0;
                    continue;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
