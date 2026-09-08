using System;
using Microsoft.DirectX;

namespace DirectBench
{
    /// <summary>
    /// CPU-side mesh construction. Run once at startup; the GPU never sees
    /// these arrays after they have been uploaded to vertex/index buffers.
    /// </summary>
    public static class GeometryBuilder
    {
        public static BuiltMesh CreateUnitSphere(int slices, int stacks)
        {
            int vertexColumns = slices + 1;
            int vertexRows = stacks + 1;
            MeshVertex[] vertices = new MeshVertex[vertexColumns * vertexRows];

            int v = 0;
            for (int stack = 0; stack <= stacks; stack++)
            {
                float vCoord = (float)stack / stacks;
                float phi = vCoord * (float)Math.PI;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                for (int slice = 0; slice <= slices; slice++)
                {
                    float uCoord = (float)slice / slices;
                    float theta = uCoord * 2.0f * (float)Math.PI;
                    float sinTheta = (float)Math.Sin(theta);
                    float cosTheta = (float)Math.Cos(theta);

                    // Left-handed: x = cos(theta)*sin(phi), y = cos(phi), z = sin(theta)*sin(phi)
                    Vector3 normal = new Vector3(cosTheta * sinPhi, cosPhi, sinTheta * sinPhi);
                    vertices[v++] = new MeshVertex
                    {
                        Position = normal,
                        Normal = normal,
                        TexCoord = new Vector2(uCoord, vCoord)
                    };
                }
            }

            int[] indices = new int[slices * stacks * 6];
            int k = 0;
            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int i0 = stack * vertexColumns + slice;
                    int i1 = i0 + vertexColumns;

                    // Clockwise from the outside, matching Direct3D 9's default CW front face.
                    indices[k++] = i0;
                    indices[k++] = i0 + 1;
                    indices[k++] = i1;

                    indices[k++] = i0 + 1;
                    indices[k++] = i1 + 1;
                    indices[k++] = i1;
                }
            }

            return new BuiltMesh(vertices, indices);
        }

        public static BuiltMesh CreateGround(float halfExtent, int divisions)
        {
            int stride = divisions + 1;
            MeshVertex[] vertices = new MeshVertex[stride * stride];

            int v = 0;
            for (int z = 0; z <= divisions; z++)
            {
                float zNorm = (float)z / divisions;
                float zPos = -halfExtent + zNorm * halfExtent * 2.0f;
                for (int x = 0; x <= divisions; x++)
                {
                    float xNorm = (float)x / divisions;
                    float xPos = -halfExtent + xNorm * halfExtent * 2.0f;
                    vertices[v++] = new MeshVertex
                    {
                        Position = new Vector3(xPos, 0.0f, zPos),
                        Normal = new Vector3(0.0f, 1.0f, 0.0f),
                        TexCoord = new Vector2(xNorm * 12.0f, zNorm * 12.0f)
                    };
                }
            }

            int[] indices = new int[divisions * divisions * 6];
            int k = 0;
            for (int z = 0; z < divisions; z++)
            {
                for (int x = 0; x < divisions; x++)
                {
                    int i0 = z * stride + x;
                    int i1 = i0 + stride;

                    indices[k++] = i0;
                    indices[k++] = i0 + 1;
                    indices[k++] = i1;

                    indices[k++] = i0 + 1;
                    indices[k++] = i1 + 1;
                    indices[k++] = i1;
                }
            }

            return new BuiltMesh(vertices, indices);
        }

        public static InstanceVertex[] CreateSphereInstances(BenchmarkOptions options)
        {
            InstanceVertex[] instances = new InstanceVertex[options.SphereCount];
            float spacing = 2.35f;
            float origin = (options.GridSize - 1) * spacing * 0.5f;
            int n = 0;
            Random rng = new Random(9);

            for (int y = 0; y < options.Layers; y++)
            {
                for (int z = 0; z < options.GridSize; z++)
                {
                    for (int x = 0; x < options.GridSize; x++)
                    {
                        float jitterX = ((float)rng.NextDouble() - 0.5f) * 0.3f;
                        float jitterZ = ((float)rng.NextDouble() - 0.5f) * 0.3f;
                        float scale = 0.72f + (float)rng.NextDouble() * 0.28f;
                        float hue = (float)rng.NextDouble();
                        Vector3 rgb = HsvToRgb(hue, 0.45f, 0.95f);
                        float spin = 0.2f + (float)rng.NextDouble() * 1.4f;
                        if (rng.Next(0, 2) == 0)
                        {
                            spin = -spin;
                        }

                        instances[n++] = new InstanceVertex
                        {
                            Position = new Vector3(
                                x * spacing - origin + jitterX,
                                1.1f + y * spacing,
                                z * spacing - origin + jitterZ),
                            Scale = scale,
                            ColorAndSpin = new Vector4(rgb.X, rgb.Y, rgb.Z, spin)
                        };
                    }
                }
            }

            return instances;
        }

        public static InstanceVertex CreateGroundInstance()
        {
            return new InstanceVertex
            {
                Position = new Vector3(0.0f, 0.0f, 0.0f),
                Scale = 1.0f,
                ColorAndSpin = new Vector4(0.55f, 0.58f, 0.62f, 0.0f)
            };
        }

        public static float SceneRadius(BenchmarkOptions options)
        {
            return (options.GridSize - 1) * 2.35f * 0.5f + 4.0f;
        }

        private static Vector3 HsvToRgb(float h, float s, float v)
        {
            float i = (float)Math.Floor(h * 6.0f);
            float f = h * 6.0f - i;
            float p = v * (1.0f - s);
            float q = v * (1.0f - f * s);
            float t = v * (1.0f - (1.0f - f) * s);
            switch ((int)i % 6)
            {
                case 0: return new Vector3(v, t, p);
                case 1: return new Vector3(q, v, p);
                case 2: return new Vector3(p, v, t);
                case 3: return new Vector3(p, q, v);
                case 4: return new Vector3(t, p, v);
                default: return new Vector3(v, p, q);
            }
        }
    }
}
