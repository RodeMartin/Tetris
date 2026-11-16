using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Tetris.Rendering
{
    public sealed class ParticleSystem : IDisposable
    {
        private const string VertexShaderPath = "Rendering/Shaders/standard.vert";
        private const string ParticleFragmentPath = "Rendering/Shaders/particle.frag";

        private const string UniformProjection = "u_Projection";
        private const string UniformModel = "u_Model";
        private const string UniformColor = "u_Color";
        private const string UniformAlpha = "u_Alpha";

        private const float ExpiredLifetime = 0.0f;
        private const float PlaneDepthZ = 0.0f;
        private const float UnitScaleZ = 1.0f;
        private const float HalfDivisor = 2.0f;
        private const float FullCircleRadians = MathF.Tau;

        private const float GravityAcceleration = 480.0f;
        private const float MinBurstSpeed = 60.0f;
        private const float MaxBurstSpeed = 240.0f;
        private const float UpwardBiasVelocity = -90.0f;
        private const float MinParticleSize = 4.0f;
        private const float MaxParticleSize = 9.0f;
        private const float DefaultLifetimeSeconds = 0.65f;
        private const int DefaultParticlesPerBurst = 7;

        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public Vector4 Color;
            public float Size;
            public float RemainingLife;
            public float MaxLife;
        }

        private readonly List<Particle> particles = new List<Particle>();
        private readonly Random random = new Random();
        private readonly Shader particleShader;

        public ParticleSystem()
        {
            particleShader = Shader.FromFiles(VertexShaderPath, ParticleFragmentPath);
        }

        public void SpawnCellBurst(Vector2 cellCenter, Vector4 color, int count = DefaultParticlesPerBurst)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (float)random.NextDouble() * FullCircleRadians;
                float speed = MinBurstSpeed + ((float)random.NextDouble() * (MaxBurstSpeed - MinBurstSpeed));
                float size = MinParticleSize + ((float)random.NextDouble() * (MaxParticleSize - MinParticleSize));

                Vector2 velocity = new Vector2(
                    MathF.Cos(angle) * speed,
                    (MathF.Sin(angle) * speed) + UpwardBiasVelocity
                );

                particles.Add(new Particle
                {
                    Position = cellCenter,
                    Velocity = velocity,
                    Color = color,
                    Size = size,
                    RemainingLife = DefaultLifetimeSeconds,
                    MaxLife = DefaultLifetimeSeconds
                });
            }
        }

        public void Update(float deltaTime)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                particle.RemainingLife -= deltaTime;

                if (particle.RemainingLife <= ExpiredLifetime)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                particle.Velocity.Y += GravityAcceleration * deltaTime;
                particle.Position += particle.Velocity * deltaTime;
                particles[i] = particle;
            }
        }

        public void Render(QuadMesh quadMesh, Matrix4 viewProjection)
        {
            if (particles.Count == 0)
            {
                return;
            }

            particleShader.Use();
            particleShader.SetMatrix4(UniformProjection, viewProjection);

            foreach (Particle particle in particles)
            {
                float normalizedLife = particle.RemainingLife / particle.MaxLife;
                float currentSize = particle.Size * normalizedLife;
                float halfSize = currentSize / HalfDivisor;

                Matrix4 scale = Matrix4.CreateScale(currentSize, currentSize, UnitScaleZ);
                Matrix4 translation = Matrix4.CreateTranslation(
                    particle.Position.X - halfSize,
                    particle.Position.Y - halfSize,
                    PlaneDepthZ
                );
                Matrix4 model = scale * translation;

                particleShader.SetMatrix4(UniformModel, model);
                particleShader.SetVector4(UniformColor, particle.Color);
                particleShader.SetFloat(UniformAlpha, normalizedLife);

                quadMesh.Render();
            }
        }

        public void Clear()
        {
            particles.Clear();
        }

        public void Dispose()
        {
            particleShader.Dispose();
        }
    }
}