using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    public sealed class CreatedDevice
    {
        public Device Device { get; set; }
        public PresentParameters PresentParameters { get; set; }
        public MultiSampleType MultiSample { get; set; }
        public CreateFlags CreateFlags { get; set; }
        public Format BackBufferFormat { get; set; }
        public DepthFormat DepthFormat { get; set; }
        public bool HardwareVertexProcessing { get; set; }
    }
}
