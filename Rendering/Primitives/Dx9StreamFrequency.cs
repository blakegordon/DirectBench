namespace DirectBench
{
    /// <summary>
    /// DWORD flags for IDirect3DDevice9::SetStreamSourceFreq.
    /// MDX exposes only the raw integer form of this API.
    /// </summary>
    public static class Dx9StreamFrequency
    {
        public const int IndexedData = 1 << 30;
        public const int InstanceData = unchecked((int)0x80000000);
    }
}
