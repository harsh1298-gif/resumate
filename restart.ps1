# Stop any running instances of the application
$processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
foreach ($process in $processes) {
    try {
        $process | Stop-Process -Force
        Write-Host "Stopped process with ID: $($process.Id)"
    }
    catch {
        Write-Host "Error stopping process $($process.Id): $_"
    }
}

# Wait for processes to close
Start-Sleep -Seconds 2

# Clear the console
Clear-Host

# Run the application
Write-Host "Starting the application..."
Set-Location $PSScriptRoot
dotnet run --launch-profile "Command Line"

# Keep the window open if there's an error
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nThe application exited with code $LASTEXITCODE. Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}
