using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    public static class InstancedDraw
    {
        public static void Draw(Device device, GpuMesh mesh, InstanceBuffer instances)
        {
            mesh.BindAsMeshStream(device);
            instances.Bind(device);

            // Stream 0 advances per index (the mesh). Stream 1 advances once per instance.
            device.SetStreamSourceFrequency(0, Dx9StreamFrequency.IndexedData | instances.Count);
            device.SetStreamSourceFrequency(1, Dx9StreamFrequency.InstanceData | 1);
            mesh.Draw(device);

            // Frequency state leaks across draws if it is not reset.
            device.SetStreamSourceFrequency(0, 1);
            device.SetStreamSourceFrequency(1, 1);
        }
    }
}
