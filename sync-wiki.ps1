# Script de synchronisation automatique vers le Wiki GitHub
# Usage : .\sync-wiki.ps1

$ErrorActionPreference = "Stop"

$repoUrl = "https://github.com/Audiothor/MusicScoreManager.wiki.git"
$wikiDir = "$PSScriptRoot\wiki"
$tempDir = "$PSScriptRoot\temp_wiki_deploy"

Write-Host "==> Déploiement du Wiki vers $repoUrl..." -ForegroundColor Cyan

if (Test-Path $tempDir) {
    Remove-Item -Recurse -Force $tempDir
}

try {
    Write-Host "==> Clonage du dépôt Wiki existant..." -ForegroundColor Yellow
    git clone $repoUrl $tempDir
}
catch {
    Write-Host "==> Le dépôt Wiki distant n'existe pas encore ou n'est pas initialisé." -ForegroundColor Red
    Write-Host "==> Pour l'initialiser : activez le Wiki sur https://github.com/Audiothor/MusicScoreManager/wiki et créez une première page 'Home'." -ForegroundColor Yellow
    exit 1
}

Write-Host "==> Copie des pages du Wiki..." -ForegroundColor Yellow
Copy-Item "$wikiDir\*" $tempDir -Recurse -Force

Set-Location $tempDir

git add .
$status = git status --porcelain
if ($status) {
    git commit -m "docs(wiki): update complete technical wiki pages"
    git push origin master
    Write-Host "==> Wiki synchronisé avec succès sur GitHub !" -ForegroundColor Green
} else {
    Write-Host "==> Aucune modification détectée dans le Wiki." -ForegroundColor Green
}

Set-Location $PSScriptRoot
Remove-Item -Recurse -Force $tempDir
