using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
 
namespace DirectBench
{
    /// <summary>
    /// Owns GPU resources for the scene and issues the per-frame draws.
    /// The hot path is two instanced DrawIndexedPrimitives calls: the sphere
    /// field and the ground. That is enough work to occupy the GPU without
    /// thousands of CPU-side draw calls.
    /// </summary>
    public sealed class SceneRenderer : IDisposable
    {
        private readonly Device _device;
        private readonly BenchmarkOptions _options;
        private readonly AdapterInfo _adapter;
        private readonly CreatedDevice _created;
        private readonly float _sceneRadius;
        private readonly float _overlayScale;
        private readonly int _hudPixels;
        private readonly int _scorePixels;
        private readonly int _hudLineHeight;

        private VertexDeclaration _declaration;
        private GpuMesh _sphere;
        private GpuMesh _ground;
        private InstanceBuffer _sphereInstances;
        private InstanceBuffer _groundInstance;
        private Texture _albedo;
        private SceneShader _shader;
        private Microsoft.DirectX.Direct3D.Font _hudFont;
        private Microsoft.DirectX.Direct3D.Font _scoreFont;
        private Sprite _hudSprite;

        private readonly Vector4[] _lightPositions = new Vector4[4];
        private readonly Vector4[] _lightColors = new Vector4[4];
        private bool _screenshotSaved;

        public Bitmap LastFrameImage { get; private set; }

        public Bitmap DetachLastFrameImage()
        {
            Bitmap image = LastFrameImage;
            LastFrameImage = null;
            return image;
        }

        public int TriangleCount { get; private set; }
        public int DrawCallsPerFrame
        {
            get { return _options.GpuPasses + 1; }
        }

        public SceneRenderer(CreatedDevice created, AdapterInfo adapter, BenchmarkOptions options, float dpiScale)
        {
            _device = created.Device;
            _created = created;
            _adapter = adapter;
            _options = options;
            _sceneRadius = GeometryBuilder.SceneRadius(options);

            // Compress DPI so 200%+ scaling stays readable without covering
            // a third of the frame. sqrt(2.5) ≈ 1.58 instead of 2.5.
            if (dpiScale < 1.0f)
            {
                dpiScale = 1.0f;
            }

            _overlayScale = (float)Math.Sqrt(dpiScale);

            _declaration = new VertexDeclaration(_device, MeshVertex.CreateInstancedElements());

            BuiltMesh sphere = GeometryBuilder.CreateUnitSphere(options.Slices, options.Stacks);
            _sphere = new GpuMesh(_device, sphere.Vertices, sphere.Indices);

            BuiltMesh ground = GeometryBuilder.CreateGround(_sceneRadius + 8.0f, 64);
            _ground = new GpuMesh(_device, ground.Vertices, ground.Indices);

            _sphereInstances = new InstanceBuffer(_device, GeometryBuilder.CreateSphereInstances(options));
            _groundInstance = new InstanceBuffer(_device, new InstanceVertex[] { GeometryBuilder.CreateGroundInstance() });

            _albedo = ProceduralTextureFactory.CreateAlbedo(_device, 256);
            _shader = new SceneShader(_device);
            _shader.BindTexture(_albedo);

            _hudPixels = Clamp(
                OverlayPx(options.Height / 48),
                OverlayPx(18),
                options.Height / 28);

            _scorePixels = Clamp(
                OverlayPx(options.Height / 26),
                OverlayPx(26),
                options.Height / 18);

            _hudFont = CreateOverlayFont(_device, _hudPixels);
            _scoreFont = CreateOverlayFont(_device, _scorePixels);
            _hudFont.PreloadText("0123456789.FPS triangles|x");
            _hudSprite = new Sprite(_device);
            _hudLineHeight = _hudPixels + OverlayPx(10);

            try
            {
                Rectangle measured = _hudFont.MeasureString(null, "Ay0.8 FPS", DrawTextFormat.SingleLine, Color.White);
                if (measured.Height > _hudPixels)
                {
                    _hudLineHeight = measured.Height + OverlayPx(8);
                }
            }
            catch (DirectXException)
            {
            }

            TriangleCount = _sphere.PrimitiveCount * _sphereInstances.Count * _options.GpuPasses + _ground.PrimitiveCount;

            _lightColors[0] = new Vector4(1.00f, 0.95f, 0.85f, 1.0f);
            _lightColors[1] = new Vector4(0.45f, 0.65f, 1.00f, 1.0f);
            _lightColors[2] = new Vector4(1.00f, 0.45f, 0.25f, 1.0f);
            _lightColors[3] = new Vector4(0.55f, 1.00f, 0.60f, 1.0f);
        }

        private int OverlayPx(int designPixels)
        {
            return Math.Max(1, (int)Math.Round(designPixels * _overlayScale));
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static Microsoft.DirectX.Direct3D.Font CreateOverlayFont(Device device, int pixelHeight)
        {
            return new Microsoft.DirectX.Direct3D.Font(
                device,
                pixelHeight,
                0,
                FontWeight.Bold,
                1,
                false,
                CharacterSet.Default,
                Precision.Default,
                FontQuality.ClearType,
                PitchAndFamily.DefaultPitch,
                "Consolas");
        }

        public void Render(FrameCounter frames, bool finished)
        {
            float time = (float)frames.ElapsedSeconds;
            Matrix viewProjection = BuildCamera(time, out Vector3 eye);
            UpdateLights(time);

            _device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.FromArgb(255, 14, 18, 28), 1.0f, 0);
            _device.BeginScene();

            _device.VertexDeclaration = _declaration;
            _shader.SetCamera(viewProjection, eye, time);
            _shader.SetLights(_lightPositions, _lightColors);

            int effectPasses = _shader.Begin();
            for (int pass = 0; pass < effectPasses; pass++)
            {
                _shader.BeginPass(pass);
                InstancedDraw.Draw(_device, _sphere, _sphereInstances);

                // Extra passes keep Z-test always-pass and disable Z writes so they
                // actually shade pixels instead of being discarded by early-Z.
                if (_options.GpuPasses > 1)
                {
                    _device.RenderState.ZBufferWriteEnable = false;
                    _device.RenderState.ZBufferFunction = Compare.Always;

                    for (int gpuPass = 1; gpuPass < _options.GpuPasses; gpuPass++)
                    {
                        InstancedDraw.Draw(_device, _sphere, _sphereInstances);
                    }

                    _device.RenderState.ZBufferFunction = Compare.LessEqual;
                    _device.RenderState.ZBufferWriteEnable = true;
                }

                InstancedDraw.Draw(_device, _ground, _groundInstance);
                _shader.EndPass();
            }

            _shader.End();

            _device.VertexShader = null;
            _device.PixelShader = null;

            if (finished)
            {
                DrawScoreBackdrop();
            }

            DrawHud(frames, finished);

            _device.EndScene();

            // Capture before Present: SwapEffect.Discard leaves the back buffer
            // undefined afterwards.
            if (finished && !_screenshotSaved)
            {
                LastFrameImage = TryCaptureBackBuffer();
                TrySaveScreenshot();
                _screenshotSaved = true;
            }

            _device.Present();
        }

        private Bitmap TryCaptureBackBuffer()
        {
            try
            {
                using (Surface backBuffer = _device.GetBackBuffer(0, 0, BackBufferType.Mono))
                using (GraphicsStream stream = SurfaceLoader.SaveToStream(ImageFileFormat.Png, backBuffer))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (Bitmap loaded = new Bitmap(stream))
                    {
                        return new Bitmap(loaded);
                    }
                }
            }
            catch (DirectXException)
            {
                return null;
            }
        }

        private void TrySaveScreenshot()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirectBench.last.png");
                if (LastFrameImage != null)
                {
                    LastFrameImage.Save(path, ImageFormat.Png);
                    return;
                }

                using (Surface backBuffer = _device.GetBackBuffer(0, 0, BackBufferType.Mono))
                {
                    SurfaceLoader.Save(path, ImageFileFormat.Png, backBuffer);
                }
            }
            catch (DirectXException)
            {
            }
            catch (System.IO.IOException)
            {
            }
        }

        private Matrix BuildCamera(float time, out Vector3 eye)
        {
            Vector3 target = new Vector3(0.0f, _options.Layers * 1.2f, 0.0f);
            float orbit = time * 0.22f;
            float radius = _sceneRadius * 1.15f;
            float height = _sceneRadius * 0.28f + 4.0f;
            eye = new Vector3(
                target.X + (float)Math.Cos(orbit) * radius,
                target.Y + height,
                target.Z + (float)Math.Sin(orbit) * radius);

            float aspect = _options.Width / (float)_options.Height;
            Matrix view = Matrix.LookAtLH(eye, target, new Vector3(0.0f, 1.0f, 0.0f));
            Matrix projection = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, aspect, 0.5f, 400.0f);
            return view * projection;
        }

        private void UpdateLights(float time)
        {
            float r = _sceneRadius * 0.9f;
            _lightPositions[0] = new Vector4((float)Math.Cos(time * 0.7f) * r, 14.0f, (float)Math.Sin(time * 0.7f) * r, 1.0f);
            _lightPositions[1] = new Vector4((float)Math.Cos(time * 0.5f + 2.1f) * r, 8.0f, (float)Math.Sin(time * 0.5f + 2.1f) * r, 1.0f);
            _lightPositions[2] = new Vector4((float)Math.Cos(time * 0.9f + 4.2f) * r * 0.6f, 18.0f, (float)Math.Sin(time * 0.9f + 4.2f) * r * 0.6f, 1.0f);
            _lightPositions[3] = new Vector4(0.0f, 22.0f, 0.0f, 1.0f);
        }

        private void DrawHud(FrameCounter frames, bool finished)
        {
            // Consolas is monospace: pad cells so '|' and the first letter of
            // each field share a column across every HUD line.
            string[][] cells;
            string[][] stats = new string[][]
            {
                new string[] { _adapter.Description, string.Format("{0}x{1}", _options.Width, _options.Height) },
                new string[] { FormatMsaa(_created.MultiSample), string.Format("{0} spheres", _options.SphereCount) },
                new string[]
                {
                    string.Format("{0:0.00}M triangles", TriangleCount / 1000000.0),
                    string.Format("{0} draws/frame", DrawCallsPerFrame)
                }
            };
            if (finished)
            {
                cells = stats;
            }
            else
            {
                cells = new string[][]
                {
                    stats[0],
                    stats[1],
                    stats[2],
                    new string[]
                    {
                        string.Format(CultureInfo.InvariantCulture, "{0:0.0} FPS (instant)", frames.InstantFps),
                        string.Format(CultureInfo.InvariantCulture, "{0:0.0} FPS (average)", frames.AverageFps)
                    },
                    new string[] { "Enter stops", "Esc exits" }
                };
            }

            int[] widths = HudColumnWidths(cells);
            string[] lines = new string[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                lines[i] = FormatHudRow(cells[i], widths);
            }

            int hudMargin = OverlayPx(12);
            int lineStep = _hudLineHeight;
            int rowWidth = _options.Width - hudMargin * 2;
            Color hudColor = Color.FromArgb(255, 240, 240, 240);

            _hudSprite.Begin(SpriteFlags.AlphaBlend);
            for (int i = 0; i < lines.Length; i++)
            {
                Rectangle row = new Rectangle(hudMargin, hudMargin + i * lineStep, rowWidth, lineStep);
                _hudFont.DrawText(_hudSprite, lines[i], row, DrawTextFormat.Left | DrawTextFormat.Top | DrawTextFormat.SingleLine | DrawTextFormat.NoClip, hudColor);
            }

            if (finished)
            {
                int margin = OverlayPx(14);
                int captionHeight = _hudPixels + OverlayPx(4);
                int scoreHeight = _scorePixels + OverlayPx(6);
                int captionY = _options.Height - margin - captionHeight;
                Rectangle scoreArea = new Rectangle(
                    margin,
                    captionY - scoreHeight,
                    _options.Width - margin * 2,
                    scoreHeight);
                _scoreFont.DrawText(
                    _hudSprite,
                    string.Format(CultureInfo.InvariantCulture, "{0:0.0} FPS", frames.AverageFps),
                    scoreArea,
                    DrawTextFormat.Left | DrawTextFormat.Bottom,
                    Color.FromArgb(255, 255, 230, 120));
                _hudFont.DrawText(
                    _hudSprite,
                    "average     Esc to close",
                    new Rectangle(margin, captionY, rowWidth, captionHeight),
                    DrawTextFormat.Left | DrawTextFormat.Top,
                    Color.FromArgb(255, 220, 220, 220));
            }

            _hudSprite.End();
        }

        private static int[] HudColumnWidths(string[][] rows)
        {
            int columns = 0;
            for (int r = 0; r < rows.Length; r++)
            {
                if (rows[r].Length > columns)
                {
                    columns = rows[r].Length;
                }
            }

            int[] widths = new int[columns];
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    int length = rows[r][c] == null ? 0 : rows[r][c].Length;
                    if (length > widths[c])
                    {
                        widths[c] = length;
                    }
                }
            }

            return widths;
        }

        private static string FormatHudRow(string[] cells, int[] widths)
        {
            int last = cells.Length - 1;
            while (last > 0 && string.IsNullOrEmpty(cells[last]))
            {
                last--;
            }

            System.Text.StringBuilder row = new System.Text.StringBuilder();
            for (int i = 0; i <= last; i++)
            {
                if (i > 0)
                {
                    row.Append("  |  ");
                }

                string cell = cells[i] ?? string.Empty;
                row.Append(cell);
                if (i < cells.Length - 1)
                {
                    int pad = widths[i] - cell.Length;
                    if (pad > 0)
                    {
                        row.Append(' ', pad);
                    }
                }
            }

            return row.ToString();
        }

        private void DrawScoreBackdrop()
        {
            int top = ScoreBarTop();
            int argb = Color.FromArgb(210, 8, 10, 16).ToArgb();

            CustomVertex.TransformedColored[] quad = new CustomVertex.TransformedColored[]
            {
                new CustomVertex.TransformedColored(0.0f, top, 0.0f, 1.0f, argb),
                new CustomVertex.TransformedColored(_options.Width, top, 0.0f, 1.0f, argb),
                new CustomVertex.TransformedColored(0.0f, _options.Height, 0.0f, 1.0f, argb),
                new CustomVertex.TransformedColored(_options.Width, _options.Height, 0.0f, 1.0f, argb)
            };

            _device.VertexDeclaration = null;
            _device.VertexFormat = CustomVertex.TransformedColored.Format;
            _device.SetTexture(0, null);
            _device.RenderState.AlphaBlendEnable = true;
            _device.RenderState.SourceBlend = Blend.SourceAlpha;
            _device.RenderState.DestinationBlend = Blend.InvSourceAlpha;
            _device.RenderState.ZBufferEnable = false;
            _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, 2, quad);
            _device.RenderState.ZBufferEnable = true;
            _device.RenderState.AlphaBlendEnable = false;
        }

        private int ScoreBarTop()
        {
            int margin = OverlayPx(14);
            int captionHeight = _hudPixels + OverlayPx(4);
            int scoreHeight = _scorePixels + OverlayPx(6);
            return _options.Height - margin - captionHeight - scoreHeight - OverlayPx(8);
        }

        public static string FormatMsaa(MultiSampleType type)
        {
            if (type == MultiSampleType.EightSamples) return "8x MSAA";
            if (type == MultiSampleType.FourSamples) return "4x MSAA";
            if (type == MultiSampleType.TwoSamples) return "2x MSAA";
            if (type == MultiSampleType.None) return "no MSAA";
            return type.ToString();
        }

        public void Dispose()
        {
            LastFrameImage?.Dispose();
            LastFrameImage = null;

            _hudSprite?.Dispose();
            _hudSprite = null;

            _scoreFont?.Dispose();
            _scoreFont = null;

            _hudFont?.Dispose();
            _hudFont = null;

            _shader?.Dispose();
            _shader = null;

            _albedo?.Dispose();
            _albedo = null;

            _sphereInstances?.Dispose();
            _sphereInstances = null;

            _groundInstance?.Dispose();
            _groundInstance = null;

            _sphere?.Dispose();
            _sphere = null;

            _ground?.Dispose();
            _ground = null;

            _declaration?.Dispose();
            _declaration = null;
        }
    }
}
