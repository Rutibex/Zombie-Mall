@echo off
setlocal
cd /d "%~dp0"
dotnet run --project tests\Ashen.Tests\Ashen.Tests.csproj -c Release
if errorlevel 1 goto failed
echo Core regression checks passed.
pause
exit /b 0
:failed
echo Regression checks failed. Review the output above.
pause
exit /b 1
