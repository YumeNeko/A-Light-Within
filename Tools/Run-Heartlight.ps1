param(
    [ValidateSet('Validate','Build','Test')][string]$Action='Validate',
    [string]$Unity='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$logs=Join-Path $project 'QA\Logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$log=Join-Path $logs ("heartlight-$Action-$stamp.log")
if($Action -eq 'Test'){
    $exe=Join-Path $project 'Builds\Windows\A Light Within.exe'
    if(-not (Test-Path -LiteralPath $exe)){throw 'New build is missing. Build Heartlight first; do not substitute the old BUILD folder.'}
    $report=Join-Path $project ('QA\Captures\Runtime-'+$stamp)
    $arguments=@('-heartlightTest','-heartlightOutput',('"'+$report+'"'),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+$log+'"'))
}else{
    if(-not (Test-Path -LiteralPath $Unity)){throw 'Unity 6000.3.10f1 executable was not found.'}
    if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close the Unity Editor before using batch mode; use the in-editor Heartlight menu if the project is already open.'}
    $method=switch($Action){
        'Validate' {'ValidateSavedScene'}
        'Build' {'BuildWindows'}
    }
    $exe=$Unity
    $arguments=@('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod',('Heartlight.Editor.HeartlightBuildTools.'+$method),'-logFile',('"'+$log+'"'))
}
$windowStyle=if($Action -eq 'Test'){'Normal'}else{'Hidden'}
$process=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $project -WindowStyle $windowStyle -PassThru
Write-Output ("Started PID="+$process.Id+"; action="+$Action)
Write-Output ("Log="+$log)
Write-Output 'This message confirms launch only, not success. Inspect the final log and process exit status.'
if($Action -eq 'Test'){Write-Output ('Expected success marker: HEARTLIGHT_RUNTIME_OK. Opt-in reports and captures: '+$report)}
elseif($Action -eq 'Validate'){Write-Output 'Expected success marker: HEARTLIGHT_STATIC_OK'}
else{Write-Output 'Expected success marker: HEARTLIGHT_BUILD_OK'}
