$ErrorActionPreference = 'Stop'

Get-Process -Name 'STHolmes.Agent','STHolmes.App','HddChecker.Agent','HddChecker.App' `
    -ErrorAction SilentlyContinue | Stop-Process -Force

foreach ($serviceName in @('STHolmes', 'HddChecker')) {
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($service -and $service.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName -Force
        $service.WaitForStatus(
            [System.ServiceProcess.ServiceControllerStatus]::Stopped,
            [TimeSpan]::FromSeconds(20))
    }
}
