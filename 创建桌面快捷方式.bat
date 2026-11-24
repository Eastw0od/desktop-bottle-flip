@echo off
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $desk = [Environment]::GetFolderPath('Desktop'); $lnk = $ws.CreateShortcut((Join-Path $desk '摸鱼立水瓶.lnk')); $lnk.TargetPath = Join-Path '%~dp0' 'DesktopBottleFlip.exe'; $lnk.WorkingDirectory = '%~dp0'; $lnk.Description = '摸鱼立水瓶 — 真实流体桌面解压挂件 (Alt+B 隐藏/呼出)'; $lnk.Save(); Write-Host '✅ 已在桌面创建快捷方式：摸鱼立水瓶.lnk'"
pause
