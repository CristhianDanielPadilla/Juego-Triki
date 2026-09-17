using System;

namespace Triki.Gameplay
{
    /// <summary>
    /// Síntesis de efectos de sonido sencillos (mono, en memoria). Se usa una vez al arrancar para
    /// tener sonido sin assets; cualquier AudioClip real asignado en el inspector lo sustituye.
    /// </summary>
    public static class ToneSynth
    {
        public const int SampleRate = 44100;

        private const float AttackSeconds = 0.005f;
        private const float ReleaseSeconds = 0.012f;
        private const float SecondHarmonic = 0.25f;

        /// <summary>Tono cuya frecuencia se desliza de <paramref name="startHz"/> a <paramref name="endHz"/>.</summary>
        public static float[] Sweep(float startHz, float endHz, float seconds, float volume, float decayPerSecond)
        {
            var samples = new float[SampleCount(seconds)];
            WriteNote(samples, 0, samples.Length, startHz, endHz, volume, decayPerSecond);
            return samples;
        }

        /// <summary>Notas seguidas, cada una con su propio ataque y caída (arpegios).</summary>
        public static float[] Sequence(float[] frequencies, float noteSeconds, float volume, float decayPerSecond)
        {
            if (frequencies == null || frequencies.Length == 0)
                throw new ArgumentException("Se necesita al menos una nota.", nameof(frequencies));

            var noteLength = SampleCount(noteSeconds);
            var samples = new float[noteLength * frequencies.Length];
            for (var i = 0; i < frequencies.Length; i++)
                WriteNote(samples, i * noteLength, noteLength, frequencies[i], frequencies[i], volume, decayPerSecond);
            return samples;
        }

        private static int SampleCount(float seconds)
        {
            if (seconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "La duración debe ser positiva.");
            return Math.Max(1, (int)(seconds * SampleRate));
        }

        private static void WriteNote(float[] buffer, int offset, int length, float startHz, float endHz, float volume, float decayPerSecond)
        {
            // Normaliza para que fundamental + armónico nunca pasen de 'volume'.
            var amplitude = Math.Min(Math.Max(volume, 0f), 1f) / (1f + SecondHarmonic);
            var attack = Math.Max(1, (int)(AttackSeconds * SampleRate));
            var release = Math.Max(1, (int)(ReleaseSeconds * SampleRate));
            var phase = 0.0;

            for (var i = 0; i < length; i++)
            {
                var progress = length > 1 ? (float)i / (length - 1) : 1f;
                var frequency = startHz + (endHz - startHz) * progress;
                phase += 2.0 * Math.PI * frequency / SampleRate;

                var time = (float)i / SampleRate;
                var envelope = (float)Math.Exp(-decayPerSecond * time);
                if (i < attack)
                    envelope *= (float)i / attack;
                var remaining = length - 1 - i;
                if (remaining < release)
                    envelope *= (float)remaining / release;

                var wave = Math.Sin(phase) + SecondHarmonic * Math.Sin(2.0 * phase);
                buffer[offset + i] = (float)(wave * amplitude * envelope);
            }
        }
    }
}
