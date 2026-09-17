@echo off
setlocal
cd /d "%~dp0"

echo ============================================
echo   Building Terkoiz.Skipper for SPT 5.0
echo ============================================
echo.

dotnet build Terkoiz.Skipper.csproj -c Release

if errorlevel 1 (
    echo.
    echo ============================================
    echo   BUILD FAILED.
    echo ============================================
    pause
    exit /b 1
)

echo.
echo ============================================
echo   BUILD SUCCEEDED.
echo   Copied to: D:\SPT-5.0.0-47242-BE\BepInEx\plugins\Terkoiz.Skipper\
echo ============================================
pause