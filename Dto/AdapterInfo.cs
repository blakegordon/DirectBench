using System;

namespace DirectBench
{
    /// <summary>
    /// One Direct3D 9 adapter as reported by Manager.Adapters.
    /// Direct3D 9 only lists adapters the runtime can present to; a headless
    /// GPU appears here only if the driver still exposes it as an adapter.
    /// </summary>
    public sealed class AdapterInfo
    {
        public int Index { get; set; }
        public string Description { get; set; }
        public string Driver { get; set; }
        public string DriverVersion { get; set; }
        public int VendorId { get; set; }
        public int DeviceId { get; set; }
        public string DisplayMode { get; set; }
        public bool MonitorAttached { get; set; }
        public bool IsDefault { get; set; }
        public Version VertexShaderVersion { get; set; }
        public Version PixelShaderVersion { get; set; }
        public int MaxPrimitiveCount { get; set; }
        public bool HardwareVertexProcessing { get; set; }
        public bool SupportsShaderModel3 { get; set; }
        public string DeviceName { get; set; }
        public int MasterAdapterOrdinal { get; set; }
        public int AdapterOrdinalInGroup { get; set; }
        public int NumberOfAdaptersInGroup { get; set; }

        public string DisplayName
        {
            get
            {
                string monitor = MonitorAttached ? DisplayMode : "no monitor (headless)";
                string mark = IsDefault ? "  [default]" : string.Empty;
                string output = string.IsNullOrEmpty(DeviceName) ? string.Empty : "  " + DeviceName;
                return string.Format("[{0}] {1}  --  {2}{3}{4}", Index, Description, monitor, output, mark);
            }
        }
    }
}
