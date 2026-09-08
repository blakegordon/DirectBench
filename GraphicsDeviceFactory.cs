using System;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    /// <summary>
    /// Builds a HAL device on a chosen adapter. Presentation interval is
    /// Immediate so vsync cannot cap the result. MSAA and depth format walk
    /// down a fallback list so a headless adapter with fewer surface formats
    /// can still come up.
    /// </summary>
    public static class GraphicsDeviceFactory
    {
        public static CreatedDevice Create(Control window, AdapterInfo adapter, BenchmarkOptions options)
        {
            if (!adapter.HardwareVertexProcessing)
            {
                throw new InvalidOperationException(
                    "Adapter '" + adapter.Description + "' has no hardware vertex processing. " +
                    "A software-vertex device would measure the CPU, not the GPU.");
            }

            if (!adapter.SupportsShaderModel3)
            {
                throw new InvalidOperationException(
                    "Adapter '" + adapter.Description + "' does not support Shader Model 3.0, " +
                    "which this benchmark uses for hardware instancing.");
            }

            Format backBufferFormat = ChooseBackBufferFormat(adapter, options.Fullscreen);
            DepthFormat depthFormat = ChooseDepthFormat(adapter, backBufferFormat);
            MultiSampleType msaa = ChooseMsaa(adapter, backBufferFormat, options.Fullscreen, options.MsaaSamples);

            PresentParameters pp = new PresentParameters
            {
                Windowed = !options.Fullscreen,
                SwapEffect = SwapEffect.Discard,
                BackBufferWidth = options.Width,
                BackBufferHeight = options.Height,
                BackBufferFormat = backBufferFormat,
                BackBufferCount = 1,
                EnableAutoDepthStencil = true,
                AutoDepthStencilFormat = depthFormat,
                PresentationInterval = PresentInterval.Immediate,
                ForceNoMultiThreadedFlag = true,
                DeviceWindow = window,
                MultiSample = msaa,
                MultiSampleQuality = 0
            };
            if (options.Fullscreen)
            {
                pp.FullScreenRefreshRateInHz = 0;
            }

            CreateFlags flags = CreateFlags.HardwareVertexProcessing;
            Device device;
            try
            {
                device = new Device(adapter.Index, DeviceType.Hardware, window, flags, pp);
            }
            catch (DirectXException firstError)
            {
                if (msaa != MultiSampleType.None)
                {
                    pp.MultiSample = MultiSampleType.None;
                    msaa = MultiSampleType.None;
                    try
                    {
                        device = new Device(adapter.Index, DeviceType.Hardware, window, flags, pp);
                    }
                    catch (DirectXException)
                    {
                        throw new InvalidOperationException(BuildCreateFailureMessage(adapter, options, firstError), firstError);
                    }
                }
                else
                {
                    throw new InvalidOperationException(BuildCreateFailureMessage(adapter, options, firstError), firstError);
                }
            }

            return new CreatedDevice
            {
                Device = device,
                PresentParameters = pp,
                MultiSample = msaa,
                CreateFlags = flags,
                BackBufferFormat = backBufferFormat,
                DepthFormat = depthFormat,
                HardwareVertexProcessing = true
            };
        }

        private static Format ChooseBackBufferFormat(AdapterInfo adapter, bool fullscreen)
        {
            Format[] candidates;
            if (!fullscreen)
            {
                try
                {
                    Format current = Manager.Adapters[adapter.Index].CurrentDisplayMode.Format;
                    candidates = new Format[] { current, Format.X8R8G8B8, Format.A8R8G8B8, Format.R5G6B5 };
                }
                catch (DirectXException)
                {
                    candidates = new Format[] { Format.X8R8G8B8, Format.A8R8G8B8, Format.R5G6B5 };
                }
            }
            else
            {
                candidates = new Format[] { Format.X8R8G8B8, Format.A8R8G8B8, Format.R5G6B5 };
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                if (Manager.CheckDeviceType(adapter.Index, DeviceType.Hardware, candidates[i], candidates[i], !fullscreen))
                {
                    return candidates[i];
                }
            }

            throw new InvalidOperationException("No usable back-buffer format on adapter " + adapter.Description + ".");
        }

        private static DepthFormat ChooseDepthFormat(AdapterInfo adapter, Format backBuffer)
        {
            Format adapterFormat = backBuffer;
            DepthFormat[] candidates = new DepthFormat[]
            {
                DepthFormat.D24S8,
                DepthFormat.D24X8,
                DepthFormat.D16
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (Manager.CheckDepthStencilMatch(adapter.Index, DeviceType.Hardware, adapterFormat, backBuffer, candidates[i]))
                {
                    return candidates[i];
                }
            }

            throw new InvalidOperationException("No usable depth buffer format on adapter " + adapter.Description + ".");
        }

        private static MultiSampleType ChooseMsaa(AdapterInfo adapter, Format backBuffer, bool fullscreen, int requestedSamples)
        {
            MultiSampleType[] ladder = new MultiSampleType[]
            {
                MultiSampleType.EightSamples,
                MultiSampleType.FourSamples,
                MultiSampleType.TwoSamples,
                MultiSampleType.None
            };

            for (int i = 0; i < ladder.Length; i++)
            {
                MultiSampleType candidate = ladder[i];
                if (SampleCount(candidate) > requestedSamples)
                {
                    continue;
                }

                if (candidate == MultiSampleType.None)
                {
                    return candidate;
                }

                if (Manager.CheckDeviceMultiSampleType(adapter.Index, DeviceType.Hardware, backBuffer, !fullscreen, candidate))
                {
                    return candidate;
                }
            }

            return MultiSampleType.None;
        }

        private static int SampleCount(MultiSampleType type)
        {
            if (type == MultiSampleType.EightSamples)
            {
                return 8;
            }

            if (type == MultiSampleType.FourSamples)
            {
                return 4;
            }

            if (type == MultiSampleType.TwoSamples)
            {
                return 2;
            }

            return 0;
        }

        private static string BuildCreateFailureMessage(AdapterInfo adapter, BenchmarkOptions options, DirectXException error)
        {
            string hint = adapter.MonitorAttached
                ? "The driver rejected device creation."
                : "This adapter has no monitor. Direct3D 9 windowed Present still needs a focus window; " +
                  "some drivers will create a HAL device on a headless GPU and blit to the desktop, others will refuse.";

            return string.Format(
                "Failed to create a Direct3D 9 HAL device on '{0}' (adapter {1}, {2}x{3}, {4}). {5}{6}Runtime error: {7}",
                adapter.Description,
                adapter.Index,
                options.Width,
                options.Height,
                options.Fullscreen ? "fullscreen" : "windowed",
                hint,
                Environment.NewLine,
                error.Message);
        }
    }
}
