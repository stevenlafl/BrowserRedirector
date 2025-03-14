@echo off
echo Building Browser Redirector Installer

REM Set paths
set WIX_PATH=C:\Program Files (x86)\WiX Toolset v3.14\bin
set BUILD_DIR=..\..\bin\Publish\win-x64
set OUTPUT_DIR=..\..\bin\Installer

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM Copy necessary files
echo Copying necessary files...
if not exist "build" mkdir "build"
if exist "build\*.*" del /Q "build\*.*"
xcopy /Y /E "%BUILD_DIR%\*.*" "build\"

REM Compile WiX source
echo Compiling installer...
"%WIX_PATH%\candle.exe" BrowserRedirector.wxs -out build\BrowserRedirector.wixobj
"%WIX_PATH%\light.exe" -ext WixUIExtension build\BrowserRedirector.wixobj -out "%OUTPUT_DIR%\BrowserRedirector-Setup.msi"

echo Installer created at %OUTPUT_DIR%\BrowserRedirector-Setup.msi

REM Copy to desktop if requested
if "%1"=="desktop" (
  echo Copying to desktop...
  copy "%OUTPUT_DIR%\BrowserRedirector-Setup.msi" "%USERPROFILE%\Desktop"
)