using System;
using OpenTK.Mathematics;

namespace Tetris.Rendering
{
    public sealed class CameraShake
    {
        private const float ZeroIntensity = 0.0f;
        private const float DecayRatePerSecond = 28.0f;
        private const float MinIntensityThreshold = 0.05f;
        private const float RandomRangeCenter = 0.5f;
        private const float RandomRangeScale = 2.0f;

        private readonly Random random = new Random();
        private float currentIntensity;

        public Vector2 CurrentOffset { get; private set; } = Vector2.Zero;

        public void Trigger(float intensity)
        {
            currentIntensity = Math.Max(currentIntensity, intensity);
        }

        public void Update(float deltaTime)
        {
            if (currentIntensity <= MinIntensityThreshold)
            {
                currentIntensity = ZeroIntensity;
                CurrentOffset = Vector2.Zero;
                return;
            }

            float offsetX = ((float)random.NextDouble() - RandomRangeCenter) * RandomRangeScale * currentIntensity;
            float offsetY = ((float)random.NextDouble() - RandomRangeCenter) * RandomRangeScale * currentIntensity;
            CurrentOffset = new Vector2(offsetX, offsetY);

            currentIntensity = Math.Max(ZeroIntensity, currentIntensity - (DecayRatePerSecond * deltaTime));
        }
    }
}