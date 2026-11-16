using System;
using System.IO;
using System.Media;
using System.Text;

namespace Tetris.Audio
{
    public sealed class SoundEngine : IDisposable
    {
        private const int SampleRate = 44100;
        private const short BitsPerSample = 16;
        private const int BitsPerByte = 8;
        private const short MonoChannels = 1;
        private const int BytesPerSample = BitsPerSample / BitsPerByte;
        private const int WavHeaderSizeBytes = 44;
        private const int RiffChunkHeaderOffset = 36;
        private const short PcmFormatCode = 1;
        private const int Subchunk1Size = 16;
        private const long StreamStartPosition = 0L;

        private const double MaxAmplitude = 9000.0;
        private const double InitialPhase = 0.0;
        private const double HalfCycleThreshold = 0.0;
        private const double MaxNormalizedEnvelope = 1.0;
        private const double PositiveSquareWave = 1.0;
        private const double NegativeSquareWave = -1.0;
        private const double NoiseRangeScale = 2.0;
        private const double NoiseRangeOffset = 1.0;

        private const double MoveFreqHz = 320.0;
        private const double MoveDurationSec = 0.035;

        private const double RotateStartFreqHz = 440.0;
        private const double RotateEndFreqHz = 660.0;
        private const double RotateDurationSec = 0.055;

        private const double HardDropStartFreqHz = 260.0;
        private const double HardDropEndFreqHz = 75.0;
        private const double HardDropDurationSec = 0.11;

        private const double LineClearStartFreqHz = 520.0;
        private const double LineClearEndFreqHz = 1040.0;
        private const double LineClearDurationSec = 0.24;

        private const double BombStartFreqHz = 140.0;
        private const double BombEndFreqHz = 32.0;
        private const double BombDurationSec = 0.35;

        private const double GameOverStartFreqHz = 360.0;
        private const double GameOverEndFreqHz = 90.0;
        private const double GameOverDurationSec = 0.45;

        private readonly SoundPlayer moveSound;
        private readonly SoundPlayer rotateSound;
        private readonly SoundPlayer hardDropSound;
        private readonly SoundPlayer lineClearSound;
        private readonly SoundPlayer bombExplosionSound;
        private readonly SoundPlayer gameOverSound;

        public bool IsMuted { get; set; }

        public SoundEngine()
        {
            moveSound = CreateTonePlayer(MoveFreqHz, MoveFreqHz, MoveDurationSec, useNoise: false);
            rotateSound = CreateTonePlayer(RotateStartFreqHz, RotateEndFreqHz, RotateDurationSec, useNoise: false);
            hardDropSound = CreateTonePlayer(HardDropStartFreqHz, HardDropEndFreqHz, HardDropDurationSec, useNoise: false);
            lineClearSound = CreateTonePlayer(LineClearStartFreqHz, LineClearEndFreqHz, LineClearDurationSec, useNoise: false);
            bombExplosionSound = CreateTonePlayer(BombStartFreqHz, BombEndFreqHz, BombDurationSec, useNoise: true);
            gameOverSound = CreateTonePlayer(GameOverStartFreqHz, GameOverEndFreqHz, GameOverDurationSec, useNoise: false);
        }

        public void PlayMove() => PlaySafe(moveSound);
        public void PlayRotate() => PlaySafe(rotateSound);
        public void PlayHardDrop() => PlaySafe(hardDropSound);
        public void PlayLineClear() => PlaySafe(lineClearSound);
        public void PlayBombExplosion() => PlaySafe(bombExplosionSound);
        public void PlayGameOver() => PlaySafe(gameOverSound);

        private void PlaySafe(SoundPlayer player)
        {
            if (IsMuted)
            {
                return;
            }

            player.Stream?.Seek(StreamStartPosition, SeekOrigin.Begin);
            player.Play();
        }

        private static SoundPlayer CreateTonePlayer(double startFreq, double endFreq, double durationSeconds, bool useNoise)
        {
            int sampleCount = (int)(SampleRate * durationSeconds);
            int dataSize = sampleCount * BytesPerSample;
            MemoryStream stream = new MemoryStream(WavHeaderSizeBytes + dataSize);
            BinaryWriter writer = new BinaryWriter(stream);
            Random random = new Random();

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(RiffChunkHeaderOffset + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(Subchunk1Size);
            writer.Write(PcmFormatCode);
            writer.Write(MonoChannels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * MonoChannels * BytesPerSample);
            writer.Write((short)(MonoChannels * BytesPerSample));
            writer.Write(BitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            double phase = InitialPhase;
            for (int i = 0; i < sampleCount; i++)
            {
                double progress = (double)i / sampleCount;
                double currentFreq = startFreq + ((endFreq - startFreq) * progress);
                double envelope = MaxNormalizedEnvelope - progress;

                phase += (Math.Tau * currentFreq) / SampleRate;
                double wave = useNoise
                    ? (random.NextDouble() * NoiseRangeScale) - NoiseRangeOffset
                    : (Math.Sin(phase) >= HalfCycleThreshold ? PositiveSquareWave : NegativeSquareWave);

                short sampleValue = (short)(wave * MaxAmplitude * envelope);
                writer.Write(sampleValue);
            }

            writer.Flush();
            stream.Position = StreamStartPosition;

            SoundPlayer player = new SoundPlayer(stream);
            player.Load();
            return player;
        }

        public void Dispose()
        {
            moveSound.Dispose();
            rotateSound.Dispose();
            hardDropSound.Dispose();
            lineClearSound.Dispose();
            bombExplosionSound.Dispose();
            gameOverSound.Dispose();
        }
    }
}