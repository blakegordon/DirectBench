using System.Runtime.InteropServices;
using Microsoft.DirectX;

namespace DirectBench
{
    /// <summary>
    /// Per-instance attributes. Kept tiny so thousands of copies are still one
    /// DrawIndexedPrimitives call rather than thousands of CPU draw calls.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceVertex
    {
        public Vector3 Position;
        public float Scale;
        public Vector4 ColorAndSpin;

        public const int Stride = 32;
    }
}
