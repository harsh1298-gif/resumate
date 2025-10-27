# Stop any running instances
Write-Host "Stopping any running instances..."
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Start LocalDB if not running
Write-Host "Checking LocalDB status..."
$localDbRunning = sqllocaldb info MSSQLLocalDB 2>&1 | Select-String "Running"
if (-not $localDbRunning) {
    Write-Host "Starting LocalDB..."
    sqllocaldb start MSSQLLocalDB
}

# Wait for LocalDB to start
Write-Host "Waiting for services to initialize..."
Start-Sleep -Seconds 2

# Set environment variable to force port 5001
$env:ASPNETCORE_URLS = "https://localhost:5001"

# Run the application with specific port
Write-Host "`nStarting the application on https://localhost:5001 ..."
Write-Host "If the browser doesn't open automatically, please visit: https://localhost:5001"
Write-Host "`nPress Ctrl+C to stop the application`n"

try {
    dotnet run --launch-profile "ResuMate"
}
catch {
    Write-Host "An error occurred: $_"
}

# Keep the window open if there's an error
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nThe application exited with code $LASTEXITCODE. Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}
