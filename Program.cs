using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.DirectX;

namespace DirectBench
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            BenchmarkOptions options;
            try
            {
                options = CommandLineParser.Parse(args);
            }
            catch (ArgumentException ex)
            {
                ShowMessage(ex.Message, true);
                return 1;
            }

            if (options.ShowHelp)
            {
                ShowMessage(CommandLineParser.HelpText, args.Length > 0);
                return 0;
            }

            IList<AdapterInfo> adapters;
            try
            {
                adapters = GraphicsAdapterCatalog.Enumerate();
            }
            catch (DirectXException ex)
            {
                ShowMessage("Failed to enumerate Direct3D 9 adapters:\r\n" + ex.Message, true);
                return 1;
            }

            if (options.ListAdaptersOnly)
            {
                string list = GraphicsAdapterCatalog.FormatList(adapters);
                NativeMethods.TryWriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirectBench.adapters.txt"), list);
                ShowMessage(list, true);
                return 0;
            }

            if (!options.AdapterSpecified && args.Length == 0)
            {
                using (SetupForm setup = new SetupForm(adapters, options))
                {
                    if (setup.ShowDialog() != DialogResult.OK || !setup.Confirmed)
                    {
                        return 0;
                    }
                }
            }
            else
            {
                options.AutoClose = options.DurationSeconds > 0;
            }

            AdapterInfo adapter;
            try
            {
                adapter = GraphicsAdapterCatalog.Resolve(options, adapters);
            }
            catch (ArgumentException ex)
            {
                ShowMessage(ex.Message + Environment.NewLine + Environment.NewLine + GraphicsAdapterCatalog.FormatList(adapters), true);
                return 1;
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(ex.Message + Environment.NewLine + Environment.NewLine + GraphicsAdapterCatalog.FormatList(adapters), true);
                return 1;
            }

            using (BenchmarkWindow window = new BenchmarkWindow(options, adapter))
            {
                Application.Run(window);
                if (window.Completed)
                {
                    string summary = string.Format(
                        CultureInfo.InvariantCulture,
                        "DirectBench result{0}Adapter: {1}{0}Resolution: {2}x{3}{0}Average FPS: {4:0.0}{0}Frames: {5}{0}Measured seconds: {6:0.000}",
                        Environment.NewLine,
                        adapter.Description,
                        options.Width,
                        options.Height,
                        window.ResultAverageFps,
                        window.ResultFrameCount,
                        window.ResultMeasuredSeconds);
                    TryWriteConsole(summary);
                    NativeMethods.TryWriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirectBench.last.txt"), summary);
                    return 0;
                }
            }

            return 0;
        }

        private static void ShowMessage(string text, bool preferConsole)
        {
            if (preferConsole)
            {
                NativeMethods.TryAttachConsole();
                TryWriteConsole(text);
                return;
            }

            MessageBox.Show(text, "DirectBench", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void TryWriteConsole(string text)
        {
            try
            {
                Console.WriteLine(text);
            }
            catch (System.IO.IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
