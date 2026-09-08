# DirectBench

A Managed Direct3D 9 GPU benchmark for Windows. It draws a hardware-instanced field of tessellated spheres with a Shader Model 3.0 Blinn-Phong effect, 4x MSAA, and no vsync, then reports live and average FPS.

x86 only: the DirectX 9 managed assemblies and native D3DX are 32-bit.

## Requirements

- Windows (Direct3D 9 is part of the OS; `d3d9.dll` is not shipped)
- .NET Framework 4.8.1
- A GPU with Shader Model 3.0 and hardware vertex processing
- To **build**: Visual Studio with the .NET Framework 4.8.1 targeting pack

You do **not** need the DirectX SDK or the DirectX End-User Runtime. Managed DirectX 1.1, Microsoft's IntelliSense XML, and the 32-bit `d3dx9_30.dll` that Direct3DX imports live in `lib/MDX` and are copied next to the exe on build.

## Build

Open `DirectBench.sln` and build the x86 platform (Release or Debug), or:

```
msbuild DirectBench.sln /p:Configuration=Release /p:Platform=x86
```

The output is `bin\x86\Release\DirectBench.exe`.

## Run

Launch `DirectBench.exe` with no arguments to pick an adapter. Enter finishes a run and shows the average FPS on the last frame; Escape (or Q) exits.

```
DirectBench.exe --help
DirectBench.exe --list
DirectBench.exe --adapter 0 --seconds 10
```

## Dependencies in this tree

| File | Why it is here |
| --- | --- |
| `lib/MDX/Microsoft.DirectX.dll` | Core MDX types (`Vector3`, `Matrix`, …), version 1.0.2902.0 |
| `lib/MDX/Microsoft.DirectX.Direct3D.dll` | Managed Direct3D 9, version 1.0.2902.0 |
| `lib/MDX/Microsoft.DirectX.Direct3DX.dll` | D3DX (effects, font, sprite, texture loader), version 1.0.2911.0 |
| `lib/MDX/*.xml` | Microsoft's IntelliSense docs for those assemblies |
| `lib/MDX/d3dx9_30.dll` | Native D3DX9 (32-bit) imported by Direct3DX |

`Microsoft.VisualC` (for MDX's `IsConstModifier`) comes from the .NET Framework targeting pack, not this folder. See `lib/MDX/NOTICE.txt` for Microsoft copyright.

## License

DirectBench source is public domain ([The Unlicense](LICENSE)).

`lib/MDX` is Copyright Microsoft Corporation and is not under that dedication. See `lib/MDX/NOTICE.txt`.
