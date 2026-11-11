# Runs the local quality gates from CLAUDE.md section 12. There is no CI pipeline in Phase 1.
# Usage: pwsh ./scripts/verify.ps1            (full run)
#        pwsh ./scripts/verify.ps1 -SkipFrontend
param(
    [switch]$SkipFrontend,
    [switch]$SkipIntegration
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot 'src/backend/PatientManagement.sln'
$webRoot = Join-Path $repositoryRoot 'src/frontend/patient-management-web'
$failures = New-Object System.Collections.Generic.List[string]

function Invoke-Gate([string]$Name, [scriptblock]$Command) {
    Write-Host "`n==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        $script:failures.Add($Name)
        Write-Host "FAILED: $Name (exit $LASTEXITCODE)" -ForegroundColor Red
    }
}

Invoke-Gate 'Backend build (Release, warnings as errors)' { dotnet build $solution -c Release }

Invoke-Gate 'Backend unit tests' {
    dotnet test $solution -c Release --no-build --filter 'FullyQualifiedName!~IntegrationTests'
}

if (-not $SkipIntegration) {
    Invoke-Gate 'Backend integration tests (LocalDB)' {
        dotnet test (Join-Path $repositoryRoot 'src/backend/tests/PatientManagement.Infrastructure.IntegrationTests') -c Release --no-build
        dotnet test (Join-Path $repositoryRoot 'src/backend/tests/PatientManagement.API.IntegrationTests') -c Release --no-build
    }
}

Invoke-Gate 'Backend vulnerable packages' {
    $report = dotnet list $solution package --vulnerable --include-transitive
    $report | Write-Host
    if ($report -match 'has the following vulnerable packages') { $global:LASTEXITCODE = 1 } else { $global:LASTEXITCODE = 0 }
}

if (-not $SkipFrontend) {
    Push-Location $webRoot
    try {
        Invoke-Gate 'Frontend lint' { npm run lint }
        Invoke-Gate 'Frontend format check' { npm run format:check }
        Invoke-Gate 'Frontend unit tests' { npm test }
        Invoke-Gate 'Frontend build' { npm run build }
        # Ratchet: fails only when a severity count rises above the accepted baseline (risk R1 in documents/risk-decisions.md).
        Invoke-Gate 'Frontend dependency audit (no new advisories)' {
            $auditJson = npm audit --json | Out-String
            $counts = ($auditJson | ConvertFrom-Json).metadata.vulnerabilities
            $baselinePath = Join-Path $repositoryRoot 'scripts/frontend-audit-baseline.json'
            $baseline = Get-Content $baselinePath -Raw | ConvertFrom-Json
            $severities = @('critical', 'high', 'moderate', 'low')
            foreach ($severity in $severities) {
                Write-Host ("{0,-9} current {1,3}  baseline {2,3}" -f $severity, $counts.$severity, $baseline.$severity)
            }
            $rising = @($severities | Where-Object { $counts.$_ -gt $baseline.$_ })
            if ($rising.Count -gt 0) {
                Write-Host ('New advisories above the accepted baseline: ' + ($rising -join ', '))
                $global:LASTEXITCODE = 1
            }
            else {
                $global:LASTEXITCODE = 0
            }
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host ''
if ($failures.Count -eq 0) {
    Write-Host 'All gates passed.' -ForegroundColor Green
    exit 0
}

Write-Host ('Failed gates: ' + ($failures -join ', ')) -ForegroundColor Red
exit 1
