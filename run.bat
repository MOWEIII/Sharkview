@echo off
setlocal

echo ==========================================
echo      AutoRenderer Quick Start
echo ==========================================

echo.
echo [1/2] Building project...
dotnet build src/AutoRenderer/AutoRenderer.csproj
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed! Please check the errors above.
    pause
    exit /b %errorlevel%
)

echo.
echo [2/2] Starting application...
echo.
dotnet run --project src/AutoRenderer/AutoRenderer.csproj --no-build

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Application crashed or failed to start.
    pause
)
