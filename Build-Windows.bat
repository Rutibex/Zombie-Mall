@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 goto missing

dotnet publish src\Ashen.Game\Ashen.Game.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -m:1 -o Windows
if errorlevel 1 goto failed
powershell -NoProfile -ExecutionPolicy Bypass -File "packaging\Prepare-Windows.ps1" -PublishDir Windows
if errorlevel 1 goto failed

echo Windows build prepared. Run Windows\ZombieMall.exe.
if /i not "%~1"=="/nopause" pause
exit /b 0
:missing
echo .NET 10 SDK not found. Install it and try again.
if /i not "%~1"=="/nopause" pause
exit /b 1
:failed
echo Build or package failed. Review messages above.
if /i not "%~1"=="/nopause" pause
exit /b 1
