# Hardwareless FTP Resource Manager
# =================================
# Read-only access to Hardwareless FTP resources and documentation

param(
    [string]$Action = "browse",
    [string]$Path = "",
    [string]$DownloadTo = "./downloads/"
)

$script:FtpServer = "127.0.0.1"
$script:FtpPort = 21
$script:FtpUser = "testcel"
$script:FtpPassword = "MeiJosy74!"
$script:BaseUri = "ftp://$FtpServer`:$FtpPort/"

function Write-HardwarelessInfo {
    param([string]$Message, [string]$Color = "White")
    Write-Host "🎮 [Hardwareless] $Message" -ForegroundColor $Color
}

function Get-HardwarelessDirectory {
    param([string]$DirectoryPath = "")
    
    try {
        $uri = if ($DirectoryPath) { "$script:BaseUri$DirectoryPath/" } else { $script:BaseUri }
        $ftpRequest = [System.Net.FtpWebRequest]::Create($uri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($script:FtpUser, $script:FtpPassword)
        $ftpRequest.UsePassive = $true
        
        $response = $ftpRequest.GetResponse()
        $responseStream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($responseStream)
        $result = $reader.ReadToEnd()
        
        $reader.Close()
        $response.Close()
        
        return $result -split "`r?`n" | Where-Object { $_.Trim() }
    }
    catch {
        Write-HardwarelessInfo "Fehler beim Zugriff auf $DirectoryPath`: $_" "Red"
        return @()
    }
}

function Get-HardwarelessFile {
    param([string]$RemoteFile, [string]$LocalFile)
    
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create("$script:BaseUri$RemoteFile")
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($script:FtpUser, $script:FtpPassword)
        $ftpRequest.UsePassive = $true
        $ftpRequest.UseBinary = $true
        
        $response = $ftpRequest.GetResponse()
        $responseStream = $response.GetResponseStream()
        
        # Ensure directory exists
        $localDir = Split-Path $LocalFile -Parent
        if ($localDir -and -not (Test-Path $localDir)) {
            New-Item -Path $localDir -ItemType Directory -Force | Out-Null
        }
        
        $fileStream = [System.IO.File]::Create($LocalFile)
        $responseStream.CopyTo($fileStream)
        
        $fileStream.Close()
        $responseStream.Close()
        $response.Close()
        
        Write-HardwarelessInfo "Download erfolgreich: $RemoteFile → $LocalFile" "Green"
        return $true
    }
    catch {
        Write-HardwarelessInfo "Download fehlgeschlagen: $_" "Red"
        return $false
    }
}

function Show-HardwarelessResources {
    Write-HardwarelessInfo "🗂️ Hardwareless FTP Resources" "Cyan"
    Write-HardwarelessInfo "===============================" "Cyan"
    
    # Root directory
    Write-HardwarelessInfo "📂 Root Verzeichnis:" "Blue"
    $rootFiles = Get-HardwarelessDirectory ""
    $rootFiles | ForEach-Object { 
        if ($_ -match "^d") {
            $name = ($_ -split "\s+" | Select-Object -Last 1)
            Write-HardwarelessInfo "  📁 $name/" "Yellow"
        } else {
            $name = ($_ -split "\s+" | Select-Object -Last 1)
            Write-HardwarelessInfo "  📄 $name" "White"
        }
    }
    
    # Hardwareless directory
    Write-HardwarelessInfo ""
    Write-HardwarelessInfo "📂 Hardwareless Verzeichnis:" "Blue"
    $hwFiles = Get-HardwarelessDirectory "Hardwareless"
    $hwFiles | ForEach-Object { 
        if ($_ -match "^d") {
            $name = ($_ -split "\s+" | Select-Object -Last 1)
            Write-HardwarelessInfo "  📁 $name/" "Yellow"
        } else {
            $name = ($_ -split "\s+" | Select-Object -Last 1)
            Write-HardwarelessInfo "  📄 $name" "White"
        }
    }
    
    # Check subdirectories
    @("Docs", "Web") | ForEach-Object {
        Write-HardwarelessInfo ""
        Write-HardwarelessInfo "📂 Hardwareless/$_/:" "Blue"
        $subFiles = Get-HardwarelessDirectory "Hardwareless/$_"
        if ($subFiles.Count -gt 0) {
            $subFiles | ForEach-Object { 
                if ($_ -match "^d") {
                    $name = ($_ -split "\s+" | Select-Object -Last 1)
                    Write-HardwarelessInfo "  📁 $name/" "Yellow"
                } else {
                    $name = ($_ -split "\s+" | Select-Object -Last 1)
                    Write-HardwarelessInfo "  📄 $name" "White"
                }
            }
        } else {
            Write-HardwarelessInfo "  (leer)" "Gray"
        }
    }
}

function Sync-HardwarelessResources {
    Write-HardwarelessInfo "⬇️ Synchronisiere Hardwareless Resources..." "Cyan"
    
    # Create download directory
    if (-not (Test-Path $DownloadTo)) {
        New-Item -Path $DownloadTo -ItemType Directory -Force | Out-Null
    }
    
    # Download certificate if available
    if (Get-HardwarelessFile "certificate.crt" "$DownloadTo/certificate.crt") {
        Write-HardwarelessInfo "Certificate heruntergeladen" "Green"
    }
    
    Write-HardwarelessInfo "Resource-Sync abgeschlossen!" "Green"
}

# Main execution
Write-HardwarelessInfo "🎮 Hardwareless FTP Resource Manager" "Cyan"
Write-HardwarelessInfo "=====================================" "Cyan"

# Test connection
try {
    $ftpRequest = [System.Net.FtpWebRequest]::Create($script:BaseUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($script:FtpUser, $script:FtpPassword)
    $ftpRequest.UsePassive = $true
    
    $response = $ftpRequest.GetResponse()
    $response.Close()
    Write-HardwarelessInfo "✅ Verbindung zum Hardwareless FTP Server hergestellt!" "Green"
}
catch {
    Write-HardwarelessInfo "❌ FTP-Verbindung fehlgeschlagen: $_" "Red"
    exit 1
}

switch ($Action.ToLower()) {
    "browse" {
        Show-HardwarelessResources
    }
    
    "sync" {
        Sync-HardwarelessResources
    }
    
    "download" {
        if ($Path) {
            $fileName = Split-Path $Path -Leaf
            $localPath = Join-Path $DownloadTo $fileName
            Get-HardwarelessFile $Path $localPath
        } else {
            Write-HardwarelessInfo "Download benötigt -Path Parameter" "Red"
        }
    }
    
    default {
        Write-HardwarelessInfo "Verfügbare Aktionen: browse, sync, download" "Yellow"
        Write-HardwarelessInfo "Beispiele:" "Yellow"
        Write-HardwarelessInfo "  .\HardwarelessResources.ps1 -Action browse" "White"
        Write-HardwarelessInfo "  .\HardwarelessResources.ps1 -Action sync" "White"
        Write-HardwarelessInfo "  .\HardwarelessResources.ps1 -Action download -Path 'certificate.crt'" "White"
    }
}

Write-HardwarelessInfo "Session beendet!" "Cyan"