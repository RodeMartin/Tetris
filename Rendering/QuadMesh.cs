using System;
using OpenTK.Graphics.OpenGL4;

namespace Tetris.Rendering
{
    public class QuadMesh : IDisposable
    {
        // Memory-layout constants
        private const int PositionLocation = 0;
        private const int TexCoordLocation = 1;
        private const int PositionComponentCount = 2; // X, Y
        private const int TexCoordComponentCount = 2; // U, V
        private const int StrideInFloats = PositionComponentCount + TexCoordComponentCount; // 4 float / vertex
        private const int StrideInBytes = StrideInFloats * sizeof(float);
        private const int TexCoordOffsetInBytes = PositionComponentCount * sizeof(float);
        private const int IndexCount = 6; // 2 triangles * 3 vertices

        public int VAO { get; private set; }
        private readonly int vbo;
        private readonly int ebo;

        private readonly float[] vertices =
        {
            // Position (X, Y)   // UV coordinates (U, V)
            0.0f, 0.0f,         0.0f, 0.0f,
            1.0f, 0.0f,         1.0f, 0.0f,
            1.0f, 1.0f,         1.0f, 1.0f,
            0.0f, 1.0f,         0.0f, 1.0f
        };

        private readonly uint[] indices = { 0, 1, 2, 2, 3, 0 };

        public QuadMesh()
        {
            VAO = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(PositionLocation, PositionComponentCount, VertexAttribPointerType.Float, false, StrideInBytes, 0);
            GL.EnableVertexAttribArray(PositionLocation);

            GL.VertexAttribPointer(TexCoordLocation, TexCoordComponentCount, VertexAttribPointerType.Float, false, StrideInBytes, TexCoordOffsetInBytes);
            GL.EnableVertexAttribArray(TexCoordLocation);

            GL.BindVertexArray(0);
        }

        public void Render()
        {
            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(VAO);
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
            GC.SuppressFinalize(this);
        }
    }
}