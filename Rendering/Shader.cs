using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Tetris.Rendering
{
    public class Shader : IDisposable
    {
        private const int CompileStatusFailed = 0;
        private const int InvalidUniformLocation = -1;

        public int Handle { get; private set; }

        public static Shader FromFiles(string vertexPath, string fragmentPath)
        {
            string vertexSource = File.ReadAllText(vertexPath);
            string fragmentSource = File.ReadAllText(fragmentPath);
            return new Shader(vertexSource, fragmentSource);
        }

        public Shader(string vertexSource, string fragmentSource)
        {
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
            if (success == CompileStatusFailed)
            {
                string log = GL.GetProgramInfoLog(Handle);
                throw new Exception($"GLSL Program Link Error:\n{log}");
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == CompileStatusFailed)
            {
                string log = GL.GetShaderInfoLog(shader);
                throw new Exception($"GLSL {type} Compile Error:\n{log}");
            }

            return shader;
        }

        public void Use() => GL.UseProgram(Handle);

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != InvalidUniformLocation)
            {
                GL.UniformMatrix4(location, false, ref matrix);
            }
        }

        public void SetVector4(string name, Vector4 vector)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != InvalidUniformLocation)
            {
                GL.Uniform4(location, vector);
            }
        }

        public void SetVector2(string name, Vector2 vector)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != InvalidUniformLocation)
            {
                GL.Uniform2(location, vector);
            }
        }

        public void SetFloat(string name, float value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != InvalidUniformLocation)
            {
                GL.Uniform1(location, value);
            }
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
            GC.SuppressFinalize(this);
        }
    }
}