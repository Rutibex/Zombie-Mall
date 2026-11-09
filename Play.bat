@echo off
setlocal
cd /d "%~dp0"
if not exist "Windows\ZombieMall.exe" (
  call Build-Windows.bat /nopause
  if errorlevel 1 (
    pause
    exit /b 1
  )
)
start "Zombie Mall" "Windows\ZombieMall.exe"
endlocal
