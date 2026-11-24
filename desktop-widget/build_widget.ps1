$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent $here
$lib = Join-Path $here "lib"

if (!(Test-Path "$lib\Microsoft.Web.WebView2.WinForms.dll")) {
    Write-Host "Downloading official Microsoft.Web.WebView2 runtime wrapper..."
    New-Item -ItemType Directory -Force -Path $lib | Out-Null
    $nupkg = "$env:TEMP\webview2_pkg.zip"
    Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2" -OutFile $nupkg
    $ext = "$env:TEMP\webview2_pkg_ext"
    if (Test-Path $ext) { Remove-Item -Recurse -Force $ext }
    Expand-Archive -Path $nupkg -DestinationPath $ext -Force
    Copy-Item "$ext\lib\net462\Microsoft.Web.WebView2.Core.dll" $lib -Force
    Copy-Item "$ext\lib\net462\Microsoft.Web.WebView2.WinForms.dll" $lib -Force
    Copy-Item "$ext\runtimes\win-x64\native\WebView2Loader.dll" $lib -Force
}

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outExe = Join-Path $repo "DesktopBottleFlip.exe"

& $csc /nologo /target:winexe /platform:x64 /optimize+ `
    /out:$outExe `
    /r:"$lib\Microsoft.Web.WebView2.Core.dll" `
    /r:"$lib\Microsoft.Web.WebView2.WinForms.dll" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    (Join-Path $here "DesktopWidget.cs")

Copy-Item "$lib\*.dll" $repo -Force
Write-Host "Built successfully -> $outExe"
