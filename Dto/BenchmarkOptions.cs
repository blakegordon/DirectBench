namespace DirectBench
{
    /// <summary>
    /// Immutable run configuration. Defaults approximate a late-era Direct3D 9
    /// scene: 1080p, 4x MSAA, a dense field of tessellated meshes, no vsync.
    /// </summary>
    public sealed class BenchmarkOptions
    {
        public int AdapterIndex { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int DurationSeconds { get; set; }
        public float WarmupSeconds { get; set; }
        public int MsaaSamples { get; set; }
        public int GridSize { get; set; }
        public int Layers { get; set; }
        public int Slices { get; set; }
        public int Stacks { get; set; }
        public int GpuPasses { get; set; }
        public bool Fullscreen { get; set; }
        public bool ListAdaptersOnly { get; set; }
        public bool ShowHelp { get; set; }
        public bool AdapterSpecified { get; set; }
        public string AdapterNameFilter { get; set; }
        public bool AutoClose { get; set; }

        public static BenchmarkOptions CreateDefault()
        {
            return new BenchmarkOptions
            {
                AdapterIndex = 0,
                Width = 1920,
                Height = 1080,
                DurationSeconds = 0,
                WarmupSeconds = 1.0f,
                MsaaSamples = 4,
                GridSize = 20,
                Layers = 4,
                Slices = 64,
                Stacks = 64,
                GpuPasses = 6,
                Fullscreen = false
            };
        }

        public int SphereCount
        {
            get { return GridSize * GridSize * Layers; }
        }

        public int TrianglesPerSphere
        {
            get { return Slices * Stacks * 2; }
        }

        public int InstancedTriangleCount
        {
            get { return SphereCount * TrianglesPerSphere; }
        }
    }
}
