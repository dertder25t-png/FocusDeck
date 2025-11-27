$ErrorActionPreference = "Stop"

Write-Host "Attempting to login..."
try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/v1/auth/login" -Method Post -ContentType "application/json" -Body '{"username":"testuser", "password":"password"}'
    $token = $loginResponse.accessToken
    Write-Host "Login successful. Token obtained."
} catch {
    Write-Host "Login failed: $_"
    exit 1
}

Write-Host "Capturing snapshot..."
try {
    $snapshotResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/context/snapshots" -Method Post -Headers @{Authorization = "Bearer $token"}
    Write-Host "Snapshot captured successfully!"
    Write-Host "Snapshot ID: $($snapshotResponse.id)"
    Write-Host "Slices count: $($snapshotResponse.slices.Count)"
} catch {
    Write-Host "Snapshot capture failed: $_"
    exit 1
}
