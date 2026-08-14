namespace Ringly.Samples.Maui;

// Generates short, looped in-memory WAV clips for the dial/ring tones — avoids needing to ship
// and license actual audio asset files for a sample app; a synthesized tone is enough to make
// "dialing" and "incoming call" audibly obvious to whoever's testing.
internal static class ToneGenerator
{
    private const int SampleRate = 8000;

    // US-style ringback cadence: 440Hz+480Hz for 2s, silence for 4s — played to the caller while
    // the callee's phone is ringing.
    public static Stream CreateRingbackTone() => new MemoryStream(BuildTone(
    [
        (440, 480, 2000),
        (0, 0, 4000)
    ]));

    // A short double-beep, distinct from the ringback tone, looped with a pause between pairs —
    // played to the callee while their own incoming-call prompt is showing.
    public static Stream CreateRingTone() => new MemoryStream(BuildTone(
    [
        (900, 0, 300),
        (0, 0, 150),
        (900, 0, 300),
        (0, 0, 1500)
    ]));

    private static byte[] BuildTone((double FrequencyHzA, double FrequencyHzB, int DurationMs)[] segments)
    {
        int totalSamples = segments.Sum(segment => SampleRate * segment.DurationMs / 1000);
        var pcm = new short[totalSamples];

        int sampleIndex = 0;

        foreach ((double frequencyHzA, double frequencyHzB, int durationMs) in segments)
        {
            int segmentSamples = SampleRate * durationMs / 1000;

            for (int i = 0; i < segmentSamples; i++)
            {
                double amplitude = frequencyHzA <= 0
                    ? 0
                    : ((Math.Sin(2 * Math.PI * frequencyHzA * sampleIndex / SampleRate) +
                        (frequencyHzB > 0 ? Math.Sin(2 * Math.PI * frequencyHzB * sampleIndex / SampleRate) : 0)) *
                       0.25);

                pcm[sampleIndex] = (short)(amplitude * short.MaxValue);
                sampleIndex++;
            }
        }

        return WriteWavFile(pcm);
    }

    private static byte[] WriteWavFile(short[] pcm)
    {
        int dataLength = pcm.Length * 2;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8);
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2); // byte rate
        writer.Write((short)2); // block align
        writer.Write((short)16); // bits per sample
        writer.Write("data"u8);
        writer.Write(dataLength);

        foreach (short sample in pcm)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
