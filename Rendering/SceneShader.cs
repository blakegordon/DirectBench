using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.IO;
using System.Reflection;

namespace DirectBench
{
    /// <summary>
    /// Compiles an in-memory .fx file with D3DX. Shader Model 3.0 is required
    /// for hardware instancing; the pixel shader is a 4-light Blinn-Phong
    /// similar to a late DirectX 9 material.
    /// </summary>
    public sealed class SceneShader : IDisposable
    {
        private const string ShaderResourceName = "DirectBench.Rendering.SceneShader.hlsl";

        private readonly Effect _effect;
        private readonly EffectHandle _technique;
        private readonly EffectHandle _viewProjection;
        private readonly EffectHandle _eyePosition;
        private readonly EffectHandle _time;
        private readonly EffectHandle _diffuseTexture;
        private readonly EffectHandle _lightPositions;
        private readonly EffectHandle _lightColors;

        public SceneShader(Device device)
        {
            string hlsl = LoadShaderSource();
            string errors = null;

            try
            {
                _effect = Effect.FromString(device, hlsl, null, null, ShaderFlags.None, null, out errors);
            }
            catch (DirectXException ex)
            {
                string detail = string.IsNullOrEmpty(errors) ? ex.Message : errors;
                throw new InvalidOperationException("HLSL compilation failed:" + Environment.NewLine + detail, ex);
            }

            if (_effect == null)
            {
                throw new InvalidOperationException("HLSL compilation returned no effect. " + errors);
            }

            _technique = _effect.GetTechnique("LitInstanced");
            _viewProjection = _effect.GetParameter(null, "ViewProjection");
            _eyePosition = _effect.GetParameter(null, "EyePosition");
            _time = _effect.GetParameter(null, "Time");
            _diffuseTexture = _effect.GetParameter(null, "DiffuseTexture");
            _lightPositions = _effect.GetParameter(null, "LightPositions");
            _lightColors = _effect.GetParameter(null, "LightColors");
            _effect.Technique = _technique;
        }

        private static string LoadShaderSource()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ShaderResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Embedded shader resource not found: " + ShaderResourceName);
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public void BindTexture(Texture texture)
        {
            _effect.SetValue(_diffuseTexture, texture);
        }

        public void SetCamera(Matrix viewProjection, Vector3 eye, float time)
        {
            _effect.SetValue(_viewProjection, viewProjection);
            _effect.SetValue(_eyePosition, new Vector4(eye.X, eye.Y, eye.Z, 1.0f));
            _effect.SetValue(_time, time);
        }

        public void SetLights(Vector4[] positions, Vector4[] colors)
        {
            _effect.SetValue(_lightPositions, positions);
            _effect.SetValue(_lightColors, colors);
        }

        public int Begin()
        {
            return _effect.Begin((FX)0);
        }

        public void BeginPass(int pass)
        {
            _effect.BeginPass(pass);
        }

        public void EndPass()
        {
            _effect.EndPass();
        }

        public void End()
        {
            _effect.End();
        }

        public void Dispose()
        {
            _effect?.Dispose();
        }
    }
}
