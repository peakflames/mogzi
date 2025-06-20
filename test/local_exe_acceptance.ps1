# This script is used to run local acceptance tests on the published executable.
# It publishes the exe to ./dist and exercises a sample of the feature switches.
# If it exits with a non-zero status code, then it is a fail; otherwise, it is a pass.

# Exit on error
$ErrorActionPreference = "Stop"

# Get the script's directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Set the project's root directory
$RootDir = Resolve-Path -Path (Join-Path $ScriptDir "..")

# Determine the runtime identifier
$rid = ""
if ($PSVersionTable.PSVersion.Major -ge 6) {
    if ($IsWindows) {
        $rid = "win-x64"
    } elseif ($IsMacOS) {
        $rid = "osx-x64"
    } elseif ($IsLinux) {
        $rid = "linux-x64"
    } else {
        Write-Error "Unsupported OS"
        exit 1
    }
} else {
    # For Windows PowerShell
    $os = (Get-CimInstance Win32_OperatingSystem).Caption
    if ($os -like "*Windows*") {
        $rid = "win-x64"
    } else {
        Write-Error "Unsupported OS for Windows PowerShell"
        exit 1
    }
}

# Publish the project
$publishDir = Join-Path $RootDir "dist/$rid"
Write-Host "Publishing for $rid to $publishDir..."
dotnet publish (Join-Path $RootDir "src/Cli/Cli.csproj") -r $rid -o $publishDir

# Path to the executable
$exePath = Join-Path $publishDir "max"
if ($rid -eq "win-x64") {
    $exePath = "$exePath.exe"
}

# Check or create the config file
$configPath = Join-Path $publishDir "maxbot.config.json"
if (-not (Test-Path $configPath)) {
    Write-Host "Creating dummy config file."
    $configContent = @"
{
    "maxbotConfig": {
        "apiProviders": [
            {
                "name": "RequestyAI",
                "type": "OpenAI-Compatible",
                "apiKey": "your-api-key-here",
                "baseUrl": "https://router.requesty.ai/v1"
            }
        ],
        "profiles": [
            {
                "default": true,
                "name": "Default",
                "apiProvider": "RequestyAI",
                "modelId": "deepinfra/microsoft/phi-4"
            }
        ]
    }
}
"@
    Set-Content -Path $configPath -Value $configContent
}

$config = Get-Content $configPath | ConvertFrom-Json
if ($config.maxbotConfig.apiProviders[0].apiKey -eq "your-api-key-here") {
    Write-Error "API key in $configPath is a placeholder. Please update it with a valid key before running the acceptance tests."
    exit 1
}

# Test cases
$testCases = @(
    @{
        Name = "Version"
        Args = @("--version")
        ExpectedOutput = "\d+\.\d+\.\d+" # SemVer
    },
    @{
        Name = "Help"
        Args = @("--help")
        ExpectedOutput = "Usage: max"
    },
    @{
        Name = "Status"
        Args = @("--status")
        ExpectedOutput = "Profile="
    },
    @{
        Name = "One-shot prompt"
        Args = @("what is the capital of michigan?")
        ExpectedOutput = "Lansing"
    },
    @{
        Name = "Piped input"
        PipedInput = "state only the word 'bruh' in all lowercase"
        Args = @("summarize this")
        ExpectedOutput = "bruh"
    }
)

# Run tests
foreach ($testCase in $testCases) {
    Write-Host ""
    Write-Host "------------------------------------------------------------------"
    Write-Host "Running test: $($testCase.Name)"
    Write-Host "------------------------------------------------------------------"
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $exePath
    $process.StartInfo.Arguments = $testCase.Args
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true

    if ($testCase.PipedInput) {
        $process.StartInfo.RedirectStandardInput = $true
    }

    $process.Start()

    if ($testCase.PipedInput) {
        $process.StandardInput.WriteLine($testCase.PipedInput)
        $process.StandardInput.Close()
    }

    $output = $process.StandardOutput.ReadToEnd()
    $errorOutput = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    Write-Host "  Exit Code: $($process.ExitCode)"
    Write-Host "  Output: $output"
    Write-Host "  Error: $errorOutput"

    if ($process.ExitCode -ne 0) {
        Write-Error "Test '$($testCase.Name)' failed with exit code $($process.ExitCode)"
        Write-Error "Error output: $errorOutput"
        exit 1
    }

    if ($output -notmatch $testCase.ExpectedOutput) {
        Write-Error "Test '$($testCase.Name)' failed. Unexpected output."
        Write-Error "Output: $output"
        exit 1
    }

    Write-Host "Test '$($testCase.Name)' passed."
}

Write-Host "All acceptance tests passed."
exit 0
