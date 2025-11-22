# Hardwareless FTP Data Exchange Module
# ======================================
# Automated file sync and data exchange for Hardwareless project
# FTP Server: 127.0.0.1:21 | User: testcel

param(
    [string]$Action = "status",
    [string]$LocalPath = "",
    [string]$RemotePath = "",
    [switch]$Force = $false
)

$script:FtpServer = "127.0.0.1"
$script:FtpPort = 21
$script:FtpUser = "testcel"
$script:FtpPassword = "MeiJosy74!"
$script:BaseUri = "ftp://$FtpServer`:$FtpPort/"

function Write-HardwarelessLog {
    param([string]$Message, [string]$Color = "White")
    Write-Host "🎮 [Hardwareless] $Message" -ForegroundColor $Color
}

function Connect-HardwarelessFTP {
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create($script:BaseUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($script:FtpUser, $script:FtpPassword)
        $ftpRequest.UsePassive = $true
        
        $response = $ftpRequest.GetResponse()
        $response.Close()
        return $true
    }
    catch {
        Write-HardwarelessLog "FTP-Verbindung fehlgeschlagen: $_" "Red"
        return $false
    }
}

function Get-HardwarelessFTPListing {
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create($script:BaseUri)
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
        Write-HardwarelessLog "Fehler beim Auflisten: $_" "Red"
        return @()
    }
}

function Upload-HardwarelessFile {
    param([string]$LocalFile, [string]$RemoteFile)
    
    if (-not (Test-Path $LocalFile)) {
        Write-HardwarelessLog "Lokale Datei nicht gefunden: $LocalFile" "Red"
        return $false
    }
    
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create("$script:BaseUri$RemoteFile")
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($script:FtpUser, $script:FtpPassword)
        $ftpRequest.UsePassive = $true
        $ftpRequest.UseBinary = $true
        
        $fileContent = [System.IO.File]::ReadAllBytes($LocalFile)
        $ftpRequest.ContentLength = $fileContent.Length
        
        $requestStream = $ftpRequest.GetRequestStream()
        $requestStream.Write($fileContent, 0, $fileContent.Length)
        $requestStream.Close()
        
        $response = $ftpRequest.GetResponse()
        $response.Close()
        
        Write-HardwarelessLog "Upload erfolgreich: $LocalFile → $RemoteFile" "Green"
        return $true
    }
    catch {
        Write-HardwarelessLog "Upload fehlgeschlagen: $_" "Red"
        return $false
    }
}

function Download-HardwarelessFile {
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
        
        Write-HardwarelessLog "Download erfolgreich: $RemoteFile → $LocalFile" "Green"
        return $true
    }
    catch {
        Write-HardwarelessLog "Download fehlgeschlagen: $_" "Red"
        return $false
    }
}

function Sync-HardwarelessProject {
    Write-HardwarelessLog "Starte Hardwareless Projekt-Synchronisation..." "Cyan"
    
    # Upload wichtige Projektdateien
    $projectFiles = @(
        @{ Local = "README.md"; Remote = "hardwareless/README.md" }
        @{ Local = "Assets/Scripts/Audio/ProceduralMusicDebugHUD.cs"; Remote = "hardwareless/ProceduralMusicDebugHUD.cs" }
        @{ Local = "Assets/Documentation/ProceduralMusic.md"; Remote = "hardwareless/ProceduralMusic.md" }
        @{ Local = "SETUP_GUIDE.md"; Remote = "hardwareless/SETUP_GUIDE.md" }
        @{ Local = "PROJECT_STATUS.md"; Remote = "hardwareless/PROJECT_STATUS.md" }
        @{ Local = "global.json"; Remote = "hardwareless/global.json" }
        @{ Local = ".gitignore"; Remote = "hardwareless/.gitignore" }
    )
    
    $successCount = 0
    $totalCount = $projectFiles.Count
    
    foreach ($file in $projectFiles) {
        if (Test-Path $file.Local) {
            if (Upload-HardwarelessFile $file.Local $file.Remote) {
                $successCount++
            }
        } else {
            Write-HardwarelessLog "Übersprungen (nicht gefunden): $($file.Local)" "Yellow"
        }
        Start-Sleep -Milliseconds 100
    }
    
    Write-HardwarelessLog "Synchronisation abgeschlossen: $successCount/$totalCount Dateien übertragen" "Green"
}

# Main script execution
Write-HardwarelessLog "Hardwareless FTP Data Exchange" "Cyan"
Write-HardwarelessLog "=============================" "Cyan"

if (-not (Connect-HardwarelessFTP)) {
    Write-HardwarelessLog "Verbindung zum FTP-Server fehlgeschlagen!" "Red"
    exit 1
}

Write-HardwarelessLog "✅ Verbindung zum Hardwareless FTP Server hergestellt!" "Green"

switch ($Action.ToLower()) {
    "status" {
        Write-HardwarelessLog "📂 FTP Server Status:" "Blue"
        $listing = Get-HardwarelessFTPListing
        if ($listing.Count -gt 0) {
            $listing | ForEach-Object { Write-HardwarelessLog "  $_" "White" }
        } else {
            Write-HardwarelessLog "  (Verzeichnis ist leer)" "Yellow"
        }
    }
    
    "sync" {
        Sync-HardwarelessProject
    }
    
    "upload" {
        if ($LocalPath -and $RemotePath) {
            Upload-HardwarelessFile $LocalPath $RemotePath
        } else {
            Write-HardwarelessLog "Upload benötigt -LocalPath und -RemotePath Parameter" "Red"
        }
    }
    
    "download" {
        if ($RemotePath -and $LocalPath) {
            Download-HardwarelessFile $RemotePath $LocalPath
        } else {
            Write-HardwarelessLog "Download benötigt -RemotePath und -LocalPath Parameter" "Red"
        }
    }
    
    default {
        Write-HardwarelessLog "Verfügbare Aktionen: status, sync, upload, download" "Yellow"
        Write-HardwarelessLog "Beispiele:" "Yellow"
        Write-HardwarelessLog "  .\HardwarelessFTP.ps1 -Action status" "White"
        Write-HardwarelessLog "  .\HardwarelessFTP.ps1 -Action sync" "White"
        Write-HardwarelessLog "  .\HardwarelessFTP.ps1 -Action upload -LocalPath 'README.md' -RemotePath 'hardwareless/README.md'" "White"
    }
}

Write-HardwarelessLog "Hardwareless FTP Session beendet!" "Cyan"