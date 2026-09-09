using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    /// <summary>
    /// Owns the swap-chain window and the Application.Idle render loop.
    /// Idle + PeekMessage is the Managed DirectX sample pattern for running
    /// unpaced by a WinForms timer.
    /// </summary>
    public sealed class BenchmarkWindow : Form
    {
        private readonly BenchmarkOptions _options;
        private readonly AdapterInfo _adapter;
        private CreatedDevice _created;
        private SceneRenderer _scene;
        private FrameCounter _frames;
        private bool _deviceReady;
        private bool _finishStarted;
        private Bitmap _resultImage;

        public double ResultAverageFps { get; private set; }
        public long ResultFrameCount { get; private set; }
        public double ResultMeasuredSeconds { get; private set; }
        public bool Completed { get; private set; }

        public BenchmarkWindow(BenchmarkOptions options, AdapterInfo adapter)
        {
            _options = options;
            _adapter = adapter;

            AutoScaleMode = AutoScaleMode.None;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Text = "DirectBench - " + adapter.Description;
            StartPosition = FormStartPosition.Manual;
            Location = Screen.PrimaryScreen.WorkingArea.Location;
            FormBorderStyle = options.Fullscreen ? FormBorderStyle.None : FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            KeyPreview = true;
            BackColor = Color.Black;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);

            int clientWidth = options.Width;
            int clientHeight = options.Height;
            if (!options.Fullscreen)
            {
                Rectangle working = Screen.PrimaryScreen.WorkingArea;
                clientWidth = Math.Min(options.Width, Math.Max(640, working.Width - 32));
                clientHeight = Math.Min(options.Height, Math.Max(480, working.Height - 64));
            }

            ClientSize = new Size(clientWidth, clientHeight);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                _created = GraphicsDeviceFactory.Create(this, _adapter, _options);
                _scene = new SceneRenderer(_created, _adapter, _options, DeviceDpi / 96.0f);
                _frames = new FrameCounter(_options.WarmupSeconds, _options.DurationSeconds);
                _frames.Start();
                _deviceReady = true;
                Application.Idle += OnIdle;
            }
            catch (DirectXException ex)
            {
                FailDeviceCreate(ex);
            }
            catch (InvalidOperationException ex)
            {
                FailDeviceCreate(ex);
            }
        }

        private void FailDeviceCreate(Exception ex)
        {
            NativeMethods.TryWriteAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirectBench.last.txt"),
                "Device creation failed:" + Environment.NewLine + ex);
            MessageBox.Show(this, ex.Message, "DirectBench - device creation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            BeginInvoke(new MethodInvoker(Close));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_resultImage == null)
            {
                return;
            }

            e.Graphics.Clear(Color.Black);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_resultImage != null)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                e.Graphics.DrawImage(_resultImage, ClientRectangle);
                return;
            }

            base.OnPaint(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter || keyData == Keys.Return)
            {
                TryStopAndScore();
                return true;
            }

            if (keyData == Keys.Q)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Q)
            {
                Close();
            }

            base.OnKeyDown(e);
        }

        private void TryStopAndScore()
        {
            if (_finishStarted || _frames == null || !_deviceReady)
            {
                return;
            }

            _frames.Complete();
            FinishBenchmark();
        }

        private void OnIdle(object sender, EventArgs e)
        {
            while (_deviceReady && NativeMethods.IsApplicationIdle())
            {
                RenderFrame();
            }
        }

        private void RenderFrame()
        {
            if (!_deviceReady || _finishStarted)
            {
                return;
            }

            if (!_created.Device.CheckCooperativeLevel())
            {
                Close();
                return;
            }

            try
            {
                _scene.Render(_frames, false);
                _frames.Tick();

                if (_frames.IsFinished)
                {
                    FinishBenchmark();
                }
            }
            catch (DirectXException ex)
            {
                _deviceReady = false;
                NativeMethods.TryWriteAllText(
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirectBench.last.txt"),
                    "Rendering failed:" + Environment.NewLine + ex);
                MessageBox.Show(this, ex.Message, "DirectBench - rendering failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        /// <summary>
        /// Draw one last frame with the average FPS in the overlay, then stop
        /// the Idle loop so the GPU goes idle. The window stays up so the last
        /// shot can be looked at; Esc or the close box exits.
        /// </summary>
        private void FinishBenchmark()
        {
            if (_finishStarted)
            {
                return;
            }

            _finishStarted = true;
            Completed = true;
            ResultAverageFps = _frames.AverageFps;
            ResultFrameCount = _frames.MeasuredFrames;
            ResultMeasuredSeconds = _frames.MeasuredSeconds;

            try
            {
                _scene.Render(_frames, true);
                _resultImage = _scene.DetachLastFrameImage();
            }
            catch (DirectXException)
            {
            }

            Application.Idle -= OnIdle;
            _deviceReady = false;
            Invalidate();

            if (_options.AutoClose)
            {
                BeginInvoke(new MethodInvoker(Close));
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.Idle -= OnIdle;
            _deviceReady = false;
            _scene?.Dispose();
            _scene = null;

            if (_created?.Device != null)
            {
                _created.Device.Dispose();
                _created.Device = null;
            }

            _resultImage?.Dispose();
            _resultImage = null;

            base.OnFormClosed(e);
        }
    }
}
