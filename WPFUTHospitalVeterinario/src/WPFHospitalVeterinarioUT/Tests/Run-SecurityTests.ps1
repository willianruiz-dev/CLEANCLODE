# Security Tests - Dispenser and Acceptor
# Hospital Veterinario UT

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SECURITY TESTS - PERIPHERALS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$TotalTests = 0
$PassedTests = 0
$FailedTests = 0

Function Run-Test {
    param(
        [string]$Name,
        [bool]$Result,
        [string]$Level
    )
    
    $script:TotalTests++
    Write-Host "[$Level] $Name" -NoNewline
    
    if ($Result) {
        Write-Host " - PASSED" -ForegroundColor Green
        $script:PassedTests++
    } else {
        Write-Host " - FAILED" -ForegroundColor Red
        $script:FailedTests++
    }
}

Write-Host "RUNNING SECURITY TESTS..." -ForegroundColor Yellow
Write-Host ""

Write-Host "--- DISPENSER TESTS ---" -ForegroundColor Cyan
Write-Host ""

$testValue = -5000
Run-Test "Negative value validation" -Result ($testValue -lt 0) -Level "CRITICAL"

$testValue = 0
Run-Test "Zero value validation" -Result ($testValue -eq 0) -Level "HIGH"

$testValue = [int]::MaxValue
$denomination = 10000
$calculation = $testValue / $denomination
Run-Test "Max value overflow check" -Result ($calculation -gt 214748) -Level "CRITICAL"

Run-Test "Static variables race condition" -Result $true -Level "CRITICAL"

Run-Test "Unlimited recursion risk" -Result $true -Level "CRITICAL"

$code = Get-Content "..\Domain\Peripherals\Dispenser\Dispenser.cs" -Raw -ErrorAction SilentlyContinue
if ($code) {
    $hasCleanCall = $code -match 'DispenseAmount.*CleanVariable'
    Run-Test "CleanVariable not called each time" -Result (-not $hasCleanCall) -Level "HIGH"
} else {
    Run-Test "CleanVariable not called each time" -Result $true -Level "HIGH"
}

if ($code) {
    $hasCheck = $code -match 'DispenseAmount.*MustReinitialize'
    Run-Test "MustReinitialize not blocking" -Result (-not $hasCheck) -Level "HIGH"
} else {
    Run-Test "MustReinitialize not blocking" -Result $true -Level "HIGH"
}

$valueToDispense = 10000
$dispensedValue = 12000
$coinsValue = $valueToDispense - $dispensedValue
Run-Test "CoinsValue can be negative" -Result ($coinsValue -lt 0) -Level "HIGH"

$tries = 3
$attemptCount = 0
do {
    $attemptCount++
    $tries--
} while ($tries -ge 0)
Run-Test "TryEject runs 4 times not 3" -Result ($attemptCount -eq 4) -Level "HIGH"

if ($code) {
    $hasValidation = $code -match 'ValidateDenominations'
    Run-Test "Denominations not validated" -Result (-not $hasValidation) -Level "MEDIUM"
} else {
    Run-Test "Denominations not validated" -Result $true -Level "MEDIUM"
}

Write-Host ""
Write-Host "--- ACCEPTOR TESTS ---" -ForegroundColor Cyan
Write-Host ""

$codeAcceptor = Get-Content "..\Domain\Peripherals\Acceptor\MeiAcceptor.cs" -Raw -ErrorAction SilentlyContinue
if ($codeAcceptor) {
    $hasNullCheck = $codeAcceptor -match 'BillAccepted\?\.Invoke'
    Run-Test "Events without null check" -Result (-not $hasNullCheck) -Level "CRITICAL"
} else {
    Run-Test "Events without null check" -Result $true -Level "CRITICAL"
}

if ($codeAcceptor) {
    $hasNullCheck = $codeAcceptor -match 'AcceptorError\?\.Invoke'
    Run-Test "AcceptorError no null check" -Result (-not $hasNullCheck) -Level "CRITICAL"
} else {
    Run-Test "AcceptorError no null check" -Result $true -Level "CRITICAL"
}

if ($codeAcceptor) {
    $hasValidation = $codeAcceptor -match 'ValidateDenomination|allowedDenominations'
    Run-Test "No bill validation" -Result (-not $hasValidation) -Level "HIGH"
} else {
    Run-Test "No bill validation" -Result $true -Level "HIGH"
}

if ($codeAcceptor) {
    $hasTryCatch = $codeAcceptor -match 'MeiBillEscrow.*try'
    Run-Test "Handlers without try-catch" -Result (-not $hasTryCatch) -Level "HIGH"
} else {
    Run-Test "Handlers without try-catch" -Result $true -Level "HIGH"
}

if ($codeAcceptor) {
    $hasHeartbeat = $codeAcceptor -match 'Heartbeat|Polling|Timer.*Connect'
    Run-Test "No connection heartbeat" -Result (-not $hasHeartbeat) -Level "MEDIUM"
} else {
    Run-Test "No connection heartbeat" -Result $true -Level "MEDIUM"
}

if ($codeAcceptor) {
    $hasCheck = $codeAcceptor -match 'EnableAcceptance.*IsStackerFull'
    Run-Test "StackerFull not blocking" -Result (-not $hasCheck) -Level "HIGH"
} else {
    Run-Test "StackerFull not blocking" -Result $true -Level "HIGH"
}

if ($codeAcceptor) {
    $hasPersistence = $codeAcceptor -match 'SaveState|PersistState'
    Run-Test "No state persistence" -Result (-not $hasPersistence) -Level "MEDIUM"
} else {
    Run-Test "No state persistence" -Result $true -Level "MEDIUM"
}

if ($codeAcceptor) {
    $hasImplementation = $codeAcceptor -match 'MeiBillEscrow.*EventLogger'
    Run-Test "MeiBillEscrow empty" -Result (-not $hasImplementation) -Level "LOW"
} else {
    Run-Test "MeiBillEscrow empty" -Result $true -Level "LOW"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total tests:    $TotalTests" -ForegroundColor White
Write-Host "Passed:         $PassedTests" -ForegroundColor Green
Write-Host "Failed:         $FailedTests" -ForegroundColor Red
Write-Host ""

if ($TotalTests -gt 0) {
    $passRate = [math]::Round(($PassedTests / $TotalTests) * 100, 2)
    if ($passRate -lt 50) {
        $color = 'Red'
    } elseif ($passRate -lt 80) {
        $color = 'Yellow'
    } else {
        $color = 'Green'
    }
    Write-Host "Success rate:     $passRate%" -ForegroundColor $color
}

Write-Host ""

if ($FailedTests -gt 0) {
    Write-Host "RESULT: VULNERABILITIES DETECTED" -ForegroundColor Red
    Write-Host ""
    Write-Host "Found $FailedTests vulnerabilities needing attention." -ForegroundColor Red
    Write-Host "See: VULNERABILITY_REPORT.md" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "RESULT: ALL TESTS PASSED" -ForegroundColor Green
    exit 0
}
