using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DirectBench
{
    public static class ProceduralTextureFactory
    {
        /// <summary>
        /// A tiled metal-plate albedo with grout lines. Built with a full mip
        /// chain so the sampler can behave like a 2004-era trilinear texture.
        /// </summary>
        public static Texture CreateAlbedo(Device device, int size)
        {
            Texture texture = new Texture(device, size, size, 0, Usage.None, Format.A8R8G8B8, Pool.Managed);
            GraphicsStream stream = texture.LockRectangle(0, LockFlags.None, out int pitch);
            try
            {
                byte[] row = new byte[size * 4];
                for (int y = 0; y < size; y++)
                {
                    int write = 0;
                    for (int x = 0; x < size; x++)
                    {
                        int tile = 32;
                        int lx = x & (tile - 1);
                        int ly = y & (tile - 1);
                        bool grout = lx < 2 || ly < 2 || lx > tile - 3 || ly > tile - 3;

                        float nx = x / (float)size;
                        float ny = y / (float)size;
                        float wave = 0.5f + 0.5f * (float)Math.Sin(nx * 27.0 + ny * 11.0);
                        int shade = grout ? 38 : (int)(118 + wave * 70.0f);

                        row[write++] = (byte)Math.Min(255, shade + 18);
                        row[write++] = (byte)Math.Min(255, shade + 8);
                        row[write++] = (byte)shade;
                        row[write++] = 255;
                    }

                    stream.Position = y * pitch;
                    stream.Write(row, 0, row.Length);
                }
            }
            finally
            {
                texture.UnlockRectangle(0);
            }

            TextureLoader.FilterTexture(texture, 0, Filter.Box);
            return texture;
        }
    }
}
