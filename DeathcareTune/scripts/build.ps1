param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$Deploy,
    [switch]$Test,
    [string]$ManagedPath,
    [string]$LocalModDir
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent

# dotnet 解析顺序：环境变量 -> 本工程 .tools -> 仓库根 .tools -> 系统 dotnet
$dotnet = $env:CSII_DOTNET
if (-not $dotnet -or -not (Test-Path -LiteralPath $dotnet)) {
    $local = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $local) { $dotnet = $local }
}
if (-not $dotnet -or -not (Test-Path -LiteralPath $dotnet)) {
    $shared = Join-Path (Split-Path $projectRoot -Parent) '.tools\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $shared) { $dotnet = $shared }
}
if (-not $dotnet -or -not (Test-Path -LiteralPath $dotnet)) {
    $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
}

if ($Deploy -and (Get-Process Cities2 -ErrorAction SilentlyContinue)) {
    throw 'Cities2 正在运行。请先正常退出游戏，再执行 -Deploy。'
}

$env:DOTNET_CLI_HOME = if (Test-Path (Join-Path $projectRoot '.tools\cli-home')) {
    Join-Path $projectRoot '.tools\cli-home'
} else {
    Join-Path (Split-Path $projectRoot -Parent) '.tools\cli-home'
}
$env:NUGET_PACKAGES = if (Test-Path (Join-Path $projectRoot '.tools\nuget-packages')) {
    Join-Path $projectRoot '.tools\nuget-packages'
} else {
    Join-Path (Split-Path $projectRoot -Parent) '.tools\nuget-packages'
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'

Push-Location $projectRoot
try {
    $buildArgs = @('build', 'DeathcareTune.csproj', '-c', $Configuration, '--nologo', '-p:DeployOnBuild=false')
    if ($ManagedPath) { $buildArgs += ('-p:ManagedPath=' + $ManagedPath) }
    if ($LocalModDir) { $buildArgs += ('-p:LocalModDir=' + $LocalModDir) }
    & $dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "mod 构建失败，退出码 $LASTEXITCODE" }

    if ($Test) {
        & $dotnet run --project 'tests\ScaleMath.Tests\ScaleMath.Tests.csproj' -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw "换算回归验证失败，退出码 $LASTEXITCODE" }
    }

    if ($Deploy) {
        $target = if ($LocalModDir) { $LocalModDir } else {
            Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\DeathcareTune'
        }
        $dll = Join-Path $projectRoot ('bin\' + $Configuration + '\DeathcareTune.dll')
        if (-not (Test-Path -LiteralPath $dll)) { throw "未找到构建产物 $dll" }
        New-Item -ItemType Directory -Force -Path $target | Out-Null
        Copy-Item -LiteralPath $dll -Destination $target -Force
        $hash = (Get-FileHash -LiteralPath (Join-Path $target 'DeathcareTune.dll') -Algorithm SHA256).Hash
        Write-Host ("已部署到 {0}" -f $target)
        Write-Host ("SHA256 {0}" -f $hash)
    }
}
finally {
    Pop-Location
}
