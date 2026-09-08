using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    public static class GraphicsAdapterCatalog
    {
        public static IList<AdapterInfo> Enumerate()
        {
            List<AdapterInfo> list = new List<AdapterInfo>();
            AdapterListCollection adapters = Manager.Adapters;
            int defaultIndex = adapters.Default.Adapter;

            for (int i = 0; i < adapters.Count; i++)
            {
                AdapterInformation adapter = adapters[i];
                AdapterDetails details = adapter.Information;
                AdapterInfo info = new AdapterInfo
                {
                    Index = adapter.Adapter,
                    Description = details.Description,
                    Driver = details.DriverName,
                    DriverVersion = details.DriverVersion.ToString(),
                    VendorId = details.VendorId,
                    DeviceId = details.DeviceId,
                    IsDefault = adapter.Adapter == defaultIndex,
                    MonitorAttached = Manager.GetAdapterMonitor(adapter.Adapter) != IntPtr.Zero,
                    DisplayMode = "(unknown)",
                    DeviceName = details.DeviceName
                };

                try
                {
                    DisplayMode mode = adapter.CurrentDisplayMode;
                    info.DisplayMode = string.Format("{0}x{1} {2}", mode.Width, mode.Height, mode.Format);
                }
                catch (DirectXException)
                {
                    info.DisplayMode = "(no display mode)";
                }

                try
                {
                    Caps caps = Manager.GetDeviceCaps(adapter.Adapter, DeviceType.Hardware);
                    info.VertexShaderVersion = caps.VertexShaderVersion;
                    info.PixelShaderVersion = caps.PixelShaderVersion;
                    info.MaxPrimitiveCount = caps.MaxPrimitiveCount;
                    info.HardwareVertexProcessing = caps.DeviceCaps.SupportsHardwareTransformAndLight;
                    info.SupportsShaderModel3 = caps.VertexShaderVersion.Major >= 3 && caps.PixelShaderVersion.Major >= 3;
                    info.MasterAdapterOrdinal = caps.MasterAdapterOrdinal;
                    info.AdapterOrdinalInGroup = caps.AdapterOrdinalInGroup;
                    info.NumberOfAdaptersInGroup = caps.NumberOfAdaptersInGroup;
                }
                catch (DirectXException)
                {
                    info.HardwareVertexProcessing = false;
                    info.SupportsShaderModel3 = false;
                }

                list.Add(info);
            }

            return list;
        }

        public static AdapterInfo Resolve(BenchmarkOptions options, IList<AdapterInfo> adapters)
        {
            if (adapters.Count == 0)
            {
                throw new InvalidOperationException("Direct3D 9 reported no adapters.");
            }

            if (!string.IsNullOrEmpty(options.AdapterNameFilter))
            {
                for (int i = 0; i < adapters.Count; i++)
                {
                    if (adapters[i].Description.IndexOf(options.AdapterNameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return adapters[i];
                    }
                }

                throw new ArgumentException("No adapter name contains \"" + options.AdapterNameFilter + "\".");
            }

            for (int i = 0; i < adapters.Count; i++)
            {
                if (adapters[i].Index == options.AdapterIndex)
                {
                    return adapters[i];
                }
            }

            throw new ArgumentException("Adapter index " + options.AdapterIndex + " is not available.");
        }

        public static string FormatList(IList<AdapterInfo> adapters)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Direct3D 9 adapters on this machine:");
            sb.AppendLine();
            for (int i = 0; i < adapters.Count; i++)
            {
                AdapterInfo a = adapters[i];
                sb.AppendLine(a.DisplayName);
                sb.AppendFormat("      Driver {0}  {1}", a.Driver, a.DriverVersion);
                sb.AppendLine();
                sb.AppendFormat("      Vendor 0x{0:X4}  Device 0x{1:X4}  VS {2}  PS {3}  MaxPrims {4}",
                    a.VendorId, a.DeviceId, FormatVersion(a.VertexShaderVersion), FormatVersion(a.PixelShaderVersion), a.MaxPrimitiveCount);
                sb.AppendLine();
                sb.AppendFormat("      Output {0}  multi-head group master={1} index={2} count={3}",
                    a.DeviceName, a.MasterAdapterOrdinal, a.AdapterOrdinalInGroup, a.NumberOfAdaptersInGroup);
                sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine("A GPU with no monitor is usable only if it appears in this list.");
            sb.AppendLine("Direct3D 9 cannot target a card the runtime does not enumerate.");
            return sb.ToString();
        }

        private static string FormatVersion(Version version)
        {
            if (version == null)
            {
                return "?";
            }

            return version.Major + "." + version.Minor;
        }
    }
}
