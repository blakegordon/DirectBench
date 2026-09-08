using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    /// <summary>
    /// Compiles an in-memory .fx file with D3DX. Shader Model 3.0 is required
    /// for hardware instancing; the pixel shader is a 4-light Blinn-Phong
    /// similar to a late DirectX 9 material.
    /// </summary>
    public sealed class SceneShader : IDisposable
    {
        private readonly Effect _effect;
        private readonly EffectHandle _technique;
        private readonly EffectHandle _viewProjection;
        private readonly EffectHandle _eyePosition;
        private readonly EffectHandle _time;
        private readonly EffectHandle _diffuseTexture;
        private readonly EffectHandle _lightPositions;
        private readonly EffectHandle _lightColors;

        private const string Hlsl = @"
float4x4 ViewProjection;
float4   EyePosition;
float    Time;
texture  DiffuseTexture;

float4 LightPositions[4];
float4 LightColors[4];

sampler2D DiffuseSampler = sampler_state
{
    Texture = <DiffuseTexture>;
    MinFilter = Anisotropic;
    MagFilter = Linear;
    MipFilter = Linear;
    MaxAnisotropy = 8;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VsInput
{
    float3 Position     : POSITION;
    float3 Normal       : NORMAL;
    float2 TexCoord     : TEXCOORD0;
    float4 InstancePos  : TEXCOORD1; // xyz = world translation, w = uniform scale
    float4 ColorAndSpin : TEXCOORD2; // rgb = albedo tint, a = Y-spin radians/sec
};

struct VsOutput
{
    float4 Position  : POSITION;
    float3 WorldPos  : TEXCOORD0;
    float3 Normal    : TEXCOORD1;
    float2 TexCoord  : TEXCOORD2;
    float3 Tint      : TEXCOORD3;
};

VsOutput VS(VsInput input)
{
    VsOutput output;

    float spin = input.ColorAndSpin.a * Time;
    float s, c;
    sincos(spin, s, c);

    // Rotate the unit-sphere vertex around Y on the GPU so animation is free
    // of per-instance CPU matrix work.
    float3 local = input.Position;
    float3 rotated = float3(local.x * c + local.z * s, local.y, -local.x * s + local.z * c);
    float3 rotatedN = float3(input.Normal.x * c + input.Normal.z * s, input.Normal.y, -input.Normal.x * s + input.Normal.z * c);

    float3 world = rotated * input.InstancePos.w + input.InstancePos.xyz;

    output.Position = mul(float4(world, 1.0), ViewProjection);
    output.WorldPos = world;
    output.Normal = rotatedN;
    output.TexCoord = input.TexCoord;
    output.Tint = input.ColorAndSpin.rgb;
    return output;
}

float3 LightOne(float3 worldPos, float3 N, float3 V, float3 lightPos, float3 lightColor)
{
    float3 toLight = lightPos - worldPos;
    float dist = length(toLight);
    float3 L = toLight / dist;
    float atten = 1.0 / (1.0 + 0.015 * dist + 0.002 * dist * dist);
    float ndotl = saturate(dot(N, L));
    float3 H = normalize(L + V);
    float spec = pow(saturate(dot(N, H)), 48.0);
    return lightColor * ((0.85 * ndotl + 0.35 * spec) * atten);
}

float4 PS(VsOutput input) : COLOR
{
    float3 N = normalize(input.Normal);
    float3 V = normalize(EyePosition.xyz - input.WorldPos);
    float3 albedo = tex2D(DiffuseSampler, input.TexCoord).rgb * input.Tint;
    float2 detailUv = input.TexCoord * 4.0 + N.xy * 0.04;
    albedo *= lerp(0.75, 1.15, tex2D(DiffuseSampler, detailUv).r);
    albedo *= lerp(0.90, 1.10, tex2D(DiffuseSampler, detailUv.yx * 1.7).g);

    float3 lit = albedo * 0.08;
    lit += albedo * LightOne(input.WorldPos, N, V, LightPositions[0].xyz, LightColors[0].rgb);
    lit += albedo * LightOne(input.WorldPos, N, V, LightPositions[1].xyz, LightColors[1].rgb);
    lit += albedo * LightOne(input.WorldPos, N, V, LightPositions[2].xyz, LightColors[2].rgb);
    lit += albedo * LightOne(input.WorldPos, N, V, LightPositions[3].xyz, LightColors[3].rgb);

    float fog = saturate(length(EyePosition.xyz - input.WorldPos) / 90.0);
    float3 fogColor = float3(0.08, 0.10, 0.16);
    return float4(lerp(lit, fogColor, fog * fog), 1.0);
}

technique LitInstanced
{
    pass P0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PS();
        ZEnable = True;
        ZWriteEnable = True;
        ZFunc = LessEqual;
        CullMode = CCW;
        AlphaBlendEnable = False;
    }
}
";

        public SceneShader(Device device)
        {
            string errors = null;
            try
            {
                _effect = Effect.FromString(device, Hlsl, null, null, ShaderFlags.None, null, out errors);
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
