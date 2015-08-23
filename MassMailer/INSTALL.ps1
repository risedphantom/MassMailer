param (
    [string]$sname = "MassMailer",
    [string]$sdname = "Служба массовых рассылок",
    [string]$sdesc = "Эта служба отправляет электронные письма в больших количествах",
    [string]$spath = "C:\Service\MassMailer\"
)

#Globals
$date = Get-Date -Format "yyy.MM.dd.HH.mm.ss"
$service = Get-Service $sname -ErrorAction SilentlyContinue
$stopFile = "DELICATE_STOP.ps1"
$servicePath = $spath
$backupFolder = "Backup"
$timeout = 10
$upgrade = $false

#Content
$exe = "MailService.exe"
$cfg = "MailService.exe.config"
$dkim = "DKIM"
$nlogCfg = "NLog.config"
$dlls = Get-ChildItem $PSScriptRoot -Filter "*.dll"

Write-Host "***MassMailer installation***"

#If service exists
if ($service){
    #Backup
    Write-Host "Found previous installation of $sname. Performing update..."
    Write-Host -NoNewline "Backup previous installation..."

    $upgrade = $true
    $servicePath = (Get-WmiObject win32_service | ?{$_.Name -eq $sname}).PathName -replace "MailService.exe", "" -replace """", ""
    $dir = New-Item -ItemType Directory -Path $servicePath$backupFolder\$date

    Copy-Item "$servicePath*" -Destination $dir -Exclude $backupFolder -Recurse -Force 
    
    Write-Host -ForegroundColor Green "[OK]"
    
    #Gracefully stop service
    if ($service.Status -eq 'running'){
        Write-Host -NoNewline "Stop $sname service..."

        $service.ExecuteCommand(128)
        
        $waitCnt = 0
        $blocked = $true
        while ($blocked -and $waitCnt -lt $timeout){
            sleep -m 1000
            $waitCnt++
            Write-Host -NoNewline "."

            $blocked = $false
            $procs = Get-Process 
            foreach($processVar in $procs.Modules){
                foreach($proc in $processVar){
                    if($proc.FileName -eq "$servicePath$exe"){
                        $blocked = $true
                    }
                }
            }
        }
        
        if ($waitCnt -eq $timeout){
            Write-Host -ForegroundColor Red "[ERROR]"
            throw "Unable to stop $sname - timeout!"
        }
        Write-Host -ForegroundColor Green "[OK]"
    }
}
else{
    #Create target dir
    Write-Host "Performing new installation of $sname..."
    Write-Host -NoNewline "Create service directory..."
    
    New-Item -ItemType Directory -Path $servicePath | Out-Null
    
    Write-Host -ForegroundColor Green "[OK]"
}

#Copy items
if ($upgrade){
    Write-Host -NoNewline "Replacing old files..."
}
else{
    Write-Host -NoNewline "Copy new files..."
}

Copy-Item $PSScriptRoot\$exe -Destination $servicePath$exe -Force
Copy-Item $PSScriptRoot\$cfg -Destination $servicePath$cfg -Force
Copy-Item  -Recurse $PSScriptRoot\$dkim -Destination $servicePath -Force
Copy-Item $PSScriptRoot\$nlogCfg -Destination $servicePath$nlogCfg -Force
foreach ($dll in $dlls){
    Copy-Item $PSScriptRoot\$dll -Destination $servicePath$dll -Force
}
"(Get-Service $sname).ExecuteCommand(128)" | Out-File $servicePath$stopFile

Write-Host -ForegroundColor Green "[OK]"

#Install service
if (-Not $upgrade){
    Write-Host -NoNewline "Install windows service..."

    New-Service -BinaryPathName $servicePath$exe -Name $sname -DisplayName $sdname -Description $sdesc -StartupType Automatic | Out-Null

    Write-Host -ForegroundColor Green "[OK]"
}
    
#Start service
Write-Host -NoNewline "Start service..."
Start-Service $sname
Write-Host -ForegroundColor Green "[OK]"

