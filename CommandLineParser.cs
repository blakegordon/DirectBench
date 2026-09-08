using System;
using System.Globalization;

namespace DirectBench
{
    public static class CommandLineParser
    {
        public static BenchmarkOptions Parse(string[] args)
        {
            BenchmarkOptions options = BenchmarkOptions.CreateDefault();
            if (args == null || args.Length == 0)
            {
                return options;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (IsFlag(arg, "--help", "-h", "/?"))
                {
                    options.ShowHelp = true;
                }
                else if (IsFlag(arg, "--list", "-l"))
                {
                    options.ListAdaptersOnly = true;
                }
                else if (IsFlag(arg, "--fullscreen", "-f"))
                {
                    options.Fullscreen = true;
                }
                else if (IsFlag(arg, "--adapter", "-a"))
                {
                    string value = RequireValue(args, ref i, arg);
                    options.AdapterSpecified = true;
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    {
                        options.AdapterIndex = index;
                    }
                    else
                    {
                        options.AdapterNameFilter = value;
                    }
                }
                else if (IsFlag(arg, "--width", "-w"))
                {
                    options.Width = ParseInt(args, ref i, arg, 320, 8192);
                }
                else if (IsFlag(arg, "--height"))
                {
                    options.Height = ParseInt(args, ref i, arg, 240, 8192);
                }
                else if (IsFlag(arg, "--seconds", "-s"))
                {
                    options.DurationSeconds = ParseInt(args, ref i, arg, 0, 3600);
                }
                else if (IsFlag(arg, "--msaa"))
                {
                    options.MsaaSamples = ParseInt(args, ref i, arg, 0, 16);
                }
                else if (IsFlag(arg, "--grid"))
                {
                    options.GridSize = ParseInt(args, ref i, arg, 1, 64);
                }
                else if (IsFlag(arg, "--layers"))
                {
                    options.Layers = ParseInt(args, ref i, arg, 1, 32);
                }
                else if (IsFlag(arg, "--slices"))
                {
                    options.Slices = ParseInt(args, ref i, arg, 4, 128);
                }
                else if (IsFlag(arg, "--stacks"))
                {
                    options.Stacks = ParseInt(args, ref i, arg, 4, 128);
                }
                else if (IsFlag(arg, "--passes"))
                {
                    options.GpuPasses = ParseInt(args, ref i, arg, 1, 64);
                }
                else
                {
                    throw new ArgumentException("Unknown argument: " + arg + Environment.NewLine + HelpText);
                }
            }

            return options;
        }

        public const string HelpText =
            "DirectBench - Managed Direct3D 9 GPU benchmark" + "\r\n" +
            "\r\n" +
            "Usage: DirectBench.exe [options]" + "\r\n" +
            "\r\n" +
            "  --list                 List Direct3D 9 adapters and exit" + "\r\n" +
            "  --adapter <n|name>     Adapter index, or a case-insensitive name substring" + "\r\n" +
            "  --width <pixels>       Back-buffer width (default 1920)" + "\r\n" +
            "  --height <pixels>      Back-buffer height (default 1080)" + "\r\n" +
            "  --seconds <n>          Stop after n seconds (0 or omit = run until Enter)" + "\r\n" +
            "  --msaa <n>             Requested MSAA samples: 0, 2, 4, 8 (default 4)" + "\r\n" +
            "  --grid <n>             Sphere count along X and Z (default 20)" + "\r\n" +
            "  --layers <n>           Sphere count along Y (default 4)" + "\r\n" +
            "  --slices <n>           Sphere longitude tessellation (default 64)" + "\r\n" +
            "  --stacks <n>           Sphere latitude tessellation (default 64)" + "\r\n" +
            "  --passes <n>           Extra instanced draws of the same field (default 6)" + "\r\n" +
            "  --fullscreen           Exclusive full screen on the chosen adapter" + "\r\n" +
            "  --help                 Show this help" + "\r\n" +
            "\r\n" +
            "With no arguments a setup dialog lists every Direct3D 9 adapter, including" + "\r\n" +
            "headless cards if the driver enumerates them.";

        private static bool IsFlag(string arg, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(arg, names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string RequireValue(string[] args, ref int index, string flag)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Missing value for " + flag);
            }

            index++;
            return args[index];
        }

        private static int ParseInt(string[] args, ref int index, string flag, int min, int max)
        {
            string text = RequireValue(args, ref index, flag);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                throw new ArgumentException("Invalid integer for " + flag + ": " + text);
            }

            if (value < min || value > max)
            {
                throw new ArgumentException(flag + " must be between " + min + " and " + max);
            }

            return value;
        }
    }
}
