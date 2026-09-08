using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DirectBench
{
    /// <summary>
    /// Shown when the process is launched with no --adapter argument so the
    /// user can pick a GPU, including a headless card if Direct3D 9 lists it.
    /// Laid out with TableLayoutPanel and AutoScaleMode.Dpi so 100% and 200%
    /// display scaling both keep labels and fields readable.
    /// </summary>
    public sealed class SetupForm : Form
    {
        private readonly IList<AdapterInfo> _adapters;
        private readonly BenchmarkOptions _options;
        private readonly ListBox _adapterList;
        private readonly ComboBox _resolution;
        private readonly ComboBox _msaa;
        private readonly NumericUpDown _seconds;
        private readonly NumericUpDown _grid;
        private readonly NumericUpDown _layers;
        private readonly NumericUpDown _passes;
        private readonly CheckBox _fullscreen;
        private readonly Label _details;

        public bool Confirmed { get; private set; }

        // Designed at 96 DPI. Scaled from DeviceDpi so 100% and 200%+ both fit.
        private const int DesignWidth = 780;
        private const int DesignHeight = 520;

        public SetupForm(IList<AdapterInfo> adapters, BenchmarkOptions options)
        {
            _adapters = adapters;
            _options = options;

            AutoScaleMode = AutoScaleMode.None;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
            Text = "DirectBench - choose a Direct3D 9 adapter";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            StartPosition = FormStartPosition.Manual;
            Location = Screen.PrimaryScreen.WorkingArea.Location;
            float scale = NativeMethods.SystemDpiScale();
            Padding = new Padding(Math.Max(8, (int)Math.Round(12 * scale)));
            ClientSize = new Size(
                (int)Math.Round(DesignWidth * scale),
                (int)Math.Round(DesignHeight * scale));

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                Padding = new Padding(0)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            Label listLabel = new Label
            {
                Text = "Adapters reported by Direct3D 9:",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            root.Controls.Add(listLabel, 0, 0);

            _adapterList = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                HorizontalScrollbar = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            for (int i = 0; i < adapters.Count; i++)
            {
                _adapterList.Items.Add(adapters[i].DisplayName);
                if (adapters[i].Index == options.AdapterIndex)
                {
                    _adapterList.SelectedIndex = i;
                }
            }

            if (_adapterList.SelectedIndex < 0 && _adapterList.Items.Count > 0)
            {
                _adapterList.SelectedIndex = 0;
            }

            _adapterList.SelectedIndexChanged += delegate { RefreshDetails(); };
            root.Controls.Add(_adapterList, 0, 1);
            UpdateListScrollWidth();

            _details = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            root.Controls.Add(_details, 0, 2);

            TableLayoutPanel optionsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 6,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };
            for (int i = 0; i < 6; i++)
            {
                optionsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 6F));
            }

            _resolution = CreateComboBox(new object[] { "1280 x 720", "1920 x 1080", "2560 x 1440", "3840 x 2160" }, IndexOfResolution(options.Width, options.Height));
            _msaa = CreateComboBox(new object[] { "None", "2x", "4x", "8x" }, IndexOfMsaa(options.MsaaSamples));
            _seconds = CreateNumeric(0, 3600, options.DurationSeconds);
            _grid = CreateNumeric(1, 64, options.GridSize);
            _layers = CreateNumeric(1, 32, options.Layers);
            _passes = CreateNumeric(1, 64, options.GpuPasses);

            optionsRow.Controls.Add(LabeledField("Resolution", _resolution), 0, 0);
            optionsRow.Controls.Add(LabeledField("MSAA", _msaa), 1, 0);
            optionsRow.Controls.Add(LabeledField("Seconds (0=Enter)", _seconds), 2, 0);
            optionsRow.Controls.Add(LabeledField("Grid (X/Z)", _grid), 3, 0);
            optionsRow.Controls.Add(LabeledField("Layers (Y)", _layers), 4, 0);
            optionsRow.Controls.Add(LabeledField("GPU passes", _passes), 5, 0);
            root.Controls.Add(optionsRow, 0, 3);

            _fullscreen = new CheckBox
            {
                Text = "Fullscreen exclusive",
                AutoSize = true,
                Checked = options.Fullscreen,
                Margin = new Padding(0, 0, 0, 8)
            };
            root.Controls.Add(_fullscreen, 0, 4);

            Label note = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                Text = "The scene is a hardware-instanced field of tessellated spheres, Shader Model 3.0 " +
                       "Blinn-Phong, no vsync, and a few GPU overdraw passes so a modern card is actually busy. " +
                       "Leave Seconds at 0 to run until Enter (then the last frame shows the average FPS). " +
                       "Escape exits. A GPU that does not appear above cannot be targeted: Direct3D 9 only " +
                       "enumerates adapters the runtime can present to."
            };
            root.Controls.Add(note, 0, 5);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = new Padding(0)
            };
            Button cancel = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(8, 0, 0, 0),
                Padding = new Padding(12, 4, 12, 4)
            };
            Button start = new Button
            {
                Text = "Start benchmark",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(8, 0, 0, 0),
                Padding = new Padding(12, 4, 12, 4)
            };
            start.Click += OnStart;
            AcceptButton = start;
            CancelButton = cancel;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(start);
            root.Controls.Add(buttons, 0, 6);

            RefreshDetails();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            float scale = DeviceDpi / 96.0f;
            Size desired = new Size(
                (int)Math.Round(DesignWidth * scale),
                (int)Math.Round(DesignHeight * scale));
            if (ClientSize != desired)
            {
                ClientSize = desired;
            }

            Location = Screen.PrimaryScreen.WorkingArea.Location;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Q)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }

            base.OnKeyDown(e);
        }

        private static ComboBox CreateComboBox(object[] items, int selectedIndex)
        {
            ComboBox box = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                IntegralHeight = false
            };
            box.Items.AddRange(items);
            box.SelectedIndex = selectedIndex;
            return box;
        }

        private static NumericUpDown CreateNumeric(int min, int max, int value)
        {
            return new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = min,
                Maximum = max,
                Value = value
            };
        }

        private static Control LabeledField(string title, Control inner)
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 8, 0)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Label label = new Label
            {
                Text = title,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };
            inner.Margin = new Padding(0);
            panel.Controls.Add(label, 0, 0);
            panel.Controls.Add(inner, 0, 1);
            return panel;
        }

        private void UpdateListScrollWidth()
        {
            int widest = _adapterList.ClientSize.Width;
            for (int i = 0; i < _adapterList.Items.Count; i++)
            {
                string text = _adapterList.Items[i].ToString();
                Size size = TextRenderer.MeasureText(text, _adapterList.Font);
                if (size.Width > widest)
                {
                    widest = size.Width;
                }
            }

            _adapterList.HorizontalExtent = widest + 12;
        }

        private void RefreshDetails()
        {
            if (_adapterList.SelectedIndex < 0 || _adapterList.SelectedIndex >= _adapters.Count)
            {
                _details.Text = string.Empty;
                return;
            }

            AdapterInfo a = _adapters[_adapterList.SelectedIndex];
            string vs = a.VertexShaderVersion == null ? "?" : a.VertexShaderVersion.Major + "." + a.VertexShaderVersion.Minor;
            string ps = a.PixelShaderVersion == null ? "?" : a.PixelShaderVersion.Major + "." + a.PixelShaderVersion.Minor;
            _details.Text = string.Format(
                "{0}\r\nDriver {1}  {2}    VS {3}  PS {4}    {5}",
                a.Description,
                a.Driver,
                a.DriverVersion,
                vs,
                ps,
                a.MonitorAttached ? "monitor attached" : "NO MONITOR - device creation may fail on this adapter");
        }

        private void OnStart(object sender, EventArgs e)
        {
            if (_adapterList.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Select an adapter.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AdapterInfo chosen = _adapters[_adapterList.SelectedIndex];
            _options.AdapterIndex = chosen.Index;
            _options.AdapterSpecified = true;
            ApplyResolution();
            _options.MsaaSamples = MsaaFromIndex(_msaa.SelectedIndex);
            _options.DurationSeconds = (int)_seconds.Value;
            _options.GridSize = (int)_grid.Value;
            _options.Layers = (int)_layers.Value;
            _options.GpuPasses = (int)_passes.Value;
            _options.Fullscreen = _fullscreen.Checked;
            Confirmed = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyResolution()
        {
            switch (_resolution.SelectedIndex)
            {
                case 0:
                    _options.Width = 1280;
                    _options.Height = 720;
                    break;
                case 2:
                    _options.Width = 2560;
                    _options.Height = 1440;
                    break;
                case 3:
                    _options.Width = 3840;
                    _options.Height = 2160;
                    break;
                default:
                    _options.Width = 1920;
                    _options.Height = 1080;
                    break;
            }
        }

        private static int IndexOfResolution(int width, int height)
        {
            if (width == 1280 && height == 720) return 0;
            if (width == 2560 && height == 1440) return 2;
            if (width == 3840 && height == 2160) return 3;
            return 1;
        }

        private static int IndexOfMsaa(int samples)
        {
            if (samples >= 8) return 3;
            if (samples >= 4) return 2;
            if (samples >= 2) return 1;
            return 0;
        }

        private static int MsaaFromIndex(int index)
        {
            if (index == 1) return 2;
            if (index == 2) return 4;
            if (index == 3) return 8;
            return 0;
        }
    }
}
