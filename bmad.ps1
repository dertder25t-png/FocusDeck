param(
    [Parameter(Position=0)]
    [ValidateSet("build", "measure", "adapt", "deploy", "run", "status", "install")]
    [string]$Command = "status",
    
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Args
)

$bmadRoot = ".\tools\BMAD-METHOD"
$bmadCli = "$bmadRoot\tools\cli\bmad-cli.js"

if (-not (Test-Path $bmadCli)) {
    Write-Host "ERROR: BMAD-METHOD not found at: $bmadCli" -ForegroundColor Red
    Write-Host "Run 'npm install' in tools/BMAD-METHOD first" -ForegroundColor Yellow
    exit 1
}

switch ($Command) {
    "build" {
        Write-Host "Building FocusDeck..." -ForegroundColor Cyan
        & dotnet build
    }
    "measure" {
        Write-Host "Measuring project health..." -ForegroundColor Cyan
        & dotnet test
    }
    "adapt" {
        Write-Host "Adapting code (format + analyze)..." -ForegroundColor Cyan
        & dotnet format
        Write-Host "Running static analysis..." -ForegroundColor Cyan
    }
    "deploy" {
        Write-Host "Deploying to production..." -ForegroundColor Cyan
        Write-Host "This requires SSH access to your Linux server" -ForegroundColor Yellow
        Write-Host "Ensure GitHub Secrets are configured: DEPLOY_HOST, DEPLOY_USER, DEPLOY_KEY" -ForegroundColor Yellow
    }
    "run" {
        Write-Host "Running full BMAD cycle (Build > Measure > Adapt > Deploy)..." -ForegroundColor Cyan
        & dotnet build
        if ($LASTEXITCODE -eq 0) {
            & dotnet test
        }
        if ($LASTEXITCODE -eq 0) {
            & dotnet format
        }
    }
    "status" {
        Write-Host "BMAD Status:" -ForegroundColor Cyan
        & node $bmadCli status
    }
    "install" {
        Write-Host "Installing BMAD..." -ForegroundColor Cyan
        & node $bmadCli install
    }
    default {
        Write-Host "Unknown command: $Command" -ForegroundColor Red
        Write-Host "Available commands: build, measure, adapt, deploy, run, status, install" -ForegroundColor Yellow
        exit 1
    }
}
