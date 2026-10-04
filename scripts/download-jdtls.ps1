$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$TargetDir = Join-Path $RepoRoot ".tools\jdtls"
$DownloadUrl = "https://download.eclipse.org/jdtls/snapshots/jdt-language-server-latest.tar.gz"

$existingLauncher = Get-ChildItem -Path (Join-Path $TargetDir "plugins") -Filter "org.eclipse.equinox.launcher_*.jar" -ErrorAction SilentlyContinue

if ($existingLauncher) {
    Write-Host "jdtls already present at $TargetDir, skipping download."
} else {
    Write-Host "Downloading jdtls to $TargetDir..."
    New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
    $Archive = Join-Path $env:TEMP "jdtls-$(New-Guid).tar.gz"
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $Archive
    tar -xzf $Archive -C $TargetDir
    Remove-Item $Archive -Force
    Write-Host "jdtls extracted."
}

$LauncherJar = (Get-ChildItem -Path (Join-Path $TargetDir "plugins") -Filter "org.eclipse.equinox.launcher_*.jar" | Select-Object -First 1).FullName
$ConfigDir = Join-Path $TargetDir "config_win"
$ApiProject = Join-Path $RepoRoot "StackDuel.Api"

Write-Host ""
Write-Host "jdtls requires Java 21+ just to run itself -- this is separate from the JDK used to"
Write-Host "analyze the code users write (which should match the Judge0 execution JDK, e.g. 17)."
Write-Host ""
Write-Host "Configure StackDuel.Api user secrets:"
Write-Host "  dotnet user-secrets set --project `"$ApiProject`" LanguageServer:Enabled true"
Write-Host "  dotnet user-secrets set --project `"$ApiProject`" LanguageServer:Java:JdtlsLauncherJarPath `"$LauncherJar`""
Write-Host "  dotnet user-secrets set --project `"$ApiProject`" LanguageServer:Java:JdtlsConfigDirectory `"$ConfigDir`""
Write-Host "  dotnet user-secrets set --project `"$ApiProject`" LanguageServer:Java:JdtlsRuntimeJavaHome `"<path to a JDK 21+ install>`""
Write-Host "  dotnet user-secrets set --project `"$ApiProject`" LanguageServer:Java:JavaHome `"<path to the JDK used for user-code analysis, e.g. the same JDK 17 Judge0 compiles with>`""
