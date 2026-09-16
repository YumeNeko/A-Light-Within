param([string]$UnityData='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Data')
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=Join-Path $project 'QA\Compile'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$references=@()
$references+=Get-ChildItem -LiteralPath (Join-Path $UnityData 'NetStandard\ref\2.1.0') -Filter '*.dll' | ForEach-Object {$_.FullName}
$references+=Get-ChildItem -LiteralPath (Join-Path $UnityData 'Managed\UnityEngine') -Filter 'UnityEngine*.dll' | ForEach-Object {$_.FullName}
$references+=Get-ChildItem -LiteralPath (Join-Path $UnityData 'Managed') -Filter 'UnityEditor*.dll' | ForEach-Object {$_.FullName}
$references+=Get-ChildItem -LiteralPath (Join-Path $project 'Library\ScriptAssemblies') -Filter '*.dll' | Where-Object {$_.Name -notmatch '^Assembly-CSharp'} | ForEach-Object {$_.FullName}
$sources=Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Recurse -Filter '*.cs' | ForEach-Object {$_.FullName}
$arguments=@('-nologo','-target:library','-langversion:latest','-unsafe','-nostdlib+','-define:UNITY_EDITOR,ENABLE_INPUT_SYSTEM,UNITY_STANDALONE_WIN,UNITY_6000_0_OR_NEWER',('-out:'+(Join-Path $output 'Heartlight.StaticCheck.dll')))
$arguments+=$references | Select-Object -Unique | ForEach-Object {'-reference:'+$_}
$arguments+=$sources
& (Join-Path $UnityData 'NetCoreRuntime\dotnet.exe') (Join-Path $UnityData 'DotNetSdkRoslyn\csc.dll') @arguments
if($LASTEXITCODE -ne 0){throw 'Static C# compilation failed'}
Write-Output 'HEARTLIGHT_CSHARP_COMPILE_OK (does not replace Unity scene/runtime validation)'
