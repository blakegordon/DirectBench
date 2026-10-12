<#
.SYNOPSIS
    Downloads Managed DirectX 1.1 and D3DX9 (d3dx9_30.dll) into lib\MDX.

.DESCRIPTION
    These Microsoft files are not stored in this repository. This script pulls
    them from Microsoft's own DirectX End-User Runtime (June 2010) package on
    download.microsoft.com, verifies its SHA-256, and extracts only the files
    DirectBench needs:

      Microsoft.DirectX.dll            1.0.2902.0  (+ .xml IntelliSense)
      Microsoft.DirectX.Direct3D.dll   1.0.2902.0  (+ .xml IntelliSense)
      Microsoft.DirectX.Direct3DX.dll  1.0.2911.0  (+ .xml IntelliSense)
      d3dx9_30.dll                     9.12.589.0  (native, 32-bit)

    DirectBench.csproj runs this automatically before resolving references when
    any of those files is missing. It is safe to run by hand.

    Runs on Windows PowerShell 5.1 using only in-box tools (expand.exe).

.PARAMETER Destination
    Output folder. Defaults to lib\MDX next to the project.

.PARAMETER Force
    Re-download even if every file is already present.
#>
[CmdletBinding()]
param(
    [string]$Destination,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest is very slow with the progress bar on PS 5.1.

# Resolved here, not as a param default: Windows PowerShell 5.1 leaves $PSScriptRoot empty there.
if (-not $Destination) {
    $Destination = Join-Path (Split-Path -Parent $PSScriptRoot) 'lib\MDX'
}

$RedistUrl    = 'https://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe'
$RedistSha256 = '053F76DCBB28802E23341B6A787E3B0791C0FA5C8D4D011B1044172DBF89C73B'

# Cabinet inside the redist -> files to take from it.
$Wanted = [ordered]@{
    'Apr2006_MDX1_x86.cab'     = @(
        'Microsoft.DirectX.dll', 'Microsoft.DirectX.xml',
        'Microsoft.DirectX.Direct3D.dll', 'Microsoft.DirectX.Direct3D.xml',
        'Microsoft.DirectX.Direct3DX.dll', 'Microsoft.DirectX.Direct3DX.xml')
    'Apr2006_d3dx9_30_x86.cab' = @('d3dx9_30.dll')
}

$allFiles = $Wanted.Values | ForEach-Object { $_ }
$missing  = @($allFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $Destination $_)) })
if (-not $Force -and $missing.Count -eq 0) {
    Write-Host "Managed DirectX already present in $Destination"
    return
}

$work = Join-Path ([IO.Path]::GetTempPath()) ('DirectBench-MDX-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    $redist = Join-Path $work 'directx_Jun2010_redist.exe'
    Write-Host "Downloading DirectX End-User Runtime (June 2010) from Microsoft (~96 MB)..."
    Invoke-WebRequest -Uri $RedistUrl -OutFile $redist -UseBasicParsing

    $hash = (Get-FileHash -LiteralPath $redist -Algorithm SHA256).Hash
    if ($hash -ne $RedistSha256) {
        throw "SHA-256 mismatch for directx_Jun2010_redist.exe. Expected $RedistSha256, got $hash."
    }

    # The redist is a self-extracting package; /Q /T: unpacks it without installing anything.
    $unpacked = Join-Path $work 'redist'
    $p = Start-Process -FilePath $redist -ArgumentList '/Q', "/T:$unpacked" -Wait -PassThru
    if ($p.ExitCode -ne 0) { throw "Extracting the DirectX redist failed (exit code $($p.ExitCode))." }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($cab in $Wanted.Keys) {
        $cabPath = Join-Path $unpacked $cab
        if (-not (Test-Path -LiteralPath $cabPath)) { throw "$cab not found in the DirectX redist." }

        $cabOut = Join-Path $work ([IO.Path]::GetFileNameWithoutExtension($cab))
        New-Item -ItemType Directory -Path $cabOut | Out-Null
        & expand.exe $cabPath "-F:*" $cabOut | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "expand.exe failed on $cab (exit code $LASTEXITCODE)." }

        foreach ($file in $Wanted[$cab]) {
            # expand.exe may lower-case names; Windows paths are case-insensitive, so copy to the canonical name.
            $src = Join-Path $cabOut $file
            if (-not (Test-Path -LiteralPath $src)) { throw "$file not found in $cab." }
            Copy-Item -LiteralPath $src -Destination (Join-Path $Destination $file) -Force
        }
    }

    Write-Host "Managed DirectX extracted to $Destination"
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
