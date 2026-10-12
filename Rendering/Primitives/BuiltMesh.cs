namespace DirectBench
{
    /// <summary>
    /// Vertices and indices for one static mesh, built on the CPU.
    /// </summary>
    public sealed class BuiltMesh
    {
        public BuiltMesh(MeshVertex[] vertices, int[] indices)
        {
            Vertices = vertices;
            Indices = indices;
        }

        public MeshVertex[] Vertices { get; }

        public int[] Indices { get; }
    }
}
