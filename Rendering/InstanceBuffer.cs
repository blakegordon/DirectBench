using System;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    /// <summary>
    /// Per-instance stream. Combined with <see cref="GpuMesh"/> via
    /// SetStreamSourceFrequency so many copies cost one draw call.
    /// </summary>
    public sealed class InstanceBuffer : IDisposable
    {
        private VertexBuffer _buffer;

        public int Count { get; private set; }

        public InstanceBuffer(Device device, InstanceVertex[] instances)
        {
            Count = instances.Length;
            _buffer = new VertexBuffer(device, instances.Length * InstanceVertex.Stride, Usage.WriteOnly, VertexFormats.None, Pool.Managed);
            _buffer.SetData(instances, 0, LockFlags.None);
        }

        public void Bind(Device device)
        {
            device.SetStreamSource(1, _buffer, 0, InstanceVertex.Stride);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
            _buffer = null;
        }
    }
}
