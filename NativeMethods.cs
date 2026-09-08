using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace DirectBench
{
    internal static class NativeMethods
    {
        private const int PmNoRemove = 0;
        private const int AttachParentProcess = -1;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public Point Point;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out NativeMessage message, IntPtr hwnd, uint min, uint max, uint remove);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        /// <summary>
        /// True when the Win32 queue is empty. Used with Application.Idle so
        /// the render loop runs as fast as the GPU/driver will accept Present
        /// calls, instead of being paced by a WinForms timer.
        /// </summary>
        public static bool IsApplicationIdle()
        {
            return !PeekMessage(out _, IntPtr.Zero, 0, 0, PmNoRemove);
        }

        public static float SystemDpiScale()
        {
            try
            {
                uint dpi = GetDpiForSystem();
                if (dpi == 0)
                {
                    return 1.0f;
                }

                return dpi / 96.0f;
            }
            catch (EntryPointNotFoundException)
            {
                return 1.0f;
            }
        }

        public static void TryAttachConsole()
        {
            if (!AttachConsole(AttachParentProcess))
            {
                AllocConsole();
            }

            try
            {
                StreamWriter writer = new StreamWriter(Console.OpenStandardOutput())
                {
                    AutoFlush = true
                };
                Console.SetOut(writer);
                Console.SetError(writer);
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public static void TryWriteAllText(string path, string contents)
        {
            try
            {
                File.WriteAllText(path, contents);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (NotSupportedException)
            {
            }
        }
    }
}
