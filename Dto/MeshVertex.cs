using System.Runtime.InteropServices;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    /// <summary>
    /// Per-vertex attributes of the unit sphere and the ground grid.
    /// 32 bytes: a typical Direct3D 9 mesh vertex.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

        public const int Stride = 32;

        public static VertexElement[] CreateMeshElements()
        {
            return new VertexElement[]
            {
                new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, 12, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 24, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                VertexElement.VertexDeclarationEnd
            };
        }

        /// <summary>
        /// Stream 0 is the mesh, stream 1 is one InstanceVertex per drawn copy.
        /// Hardware instancing (vs_3_0) reads stream 1 at instance frequency.
        /// </summary>
        public static VertexElement[] CreateInstancedElements()
        {
            return new VertexElement[]
            {
                new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, 12, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 24, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(1, 0, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
                new VertexElement(1, 16, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 2),
                VertexElement.VertexDeclarationEnd
            };
        }
    }
}
