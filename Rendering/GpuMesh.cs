using System;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    /// <summary>
    /// A static vertex+index buffer pair. Pool.Managed lets the runtime keep a
    /// system copy for device reset; Usage.WriteOnly tells the driver the CPU
    /// will not read it back.
    /// </summary>
    public sealed class GpuMesh : IDisposable
    {
        private VertexBuffer _vertices;
        private IndexBuffer _indices;

        public int VertexCount { get; private set; }
        public int PrimitiveCount { get; private set; }
        public int VertexStride { get; private set; }

        public GpuMesh(Device device, MeshVertex[] vertices, int[] indices)
        {
            VertexCount = vertices.Length;
            PrimitiveCount = indices.Length / 3;
            VertexStride = MeshVertex.Stride;

            _vertices = new VertexBuffer(device, vertices.Length * MeshVertex.Stride, Usage.WriteOnly, VertexFormats.None, Pool.Managed);
            _vertices.SetData(vertices, 0, LockFlags.None);

            _indices = new IndexBuffer(device, indices.Length * 4, Usage.WriteOnly, Pool.Managed, false);
            _indices.SetData(indices, 0, LockFlags.None);
        }

        public void BindAsMeshStream(Device device)
        {
            device.SetStreamSource(0, _vertices, 0, VertexStride);
            device.Indices = _indices;
        }

        public void Draw(Device device)
        {
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexCount, 0, PrimitiveCount);
        }

        public void Dispose()
        {
            _vertices?.Dispose();
            _vertices = null;

            _indices?.Dispose();
            _indices = null;
        }
    }
}
