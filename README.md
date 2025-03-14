# Browser Redirector

A cross-platform application that lets you redirect URLs to specific browsers based on regular expression patterns.

## Features

- Works on Windows, macOS, and Linux
- Set different browsers for different websites
- Use powerful regex patterns to match URLs
- Auto-detects installed browsers
- Fall back to default browser if needed
- Comprehensive logging

## Installation

### Download

All installation files are available on the [GitHub Releases page](https://github.com/stevenlafl/BrowserRedirector/releases/latest).

### Windows

For full functionality (including the ability to set as default browser):
1. Download the MSI installer for your system architecture:
   - `BrowserRedirector-[version]-win-x64-Setup.msi` - For 64-bit Intel/AMD systems (most common)
   - `BrowserRedirector-[version]-win-x86-Setup.msi` - For 32-bit Intel/AMD systems
   - `BrowserRedirector-[version]-win-arm64-Setup.msi` - For ARM64 systems (like Surface Pro X)
2. Run the MSI installer and follow the prompts
3. Choose whether to register Browser Redirector as a browser option in Windows

For portable use without installation:
1. Download the standalone executable for your architecture
2. Run `BrowserRedirector.exe` directly (no browser registration available)

### macOS

1. Download the appropriate installer package:
   - `BrowserRedirector-[version]-osx-x64.pkg` - For Intel Macs
   - `BrowserRedirector-[version]-osx-arm64.pkg` - For Apple Silicon Macs (M1/M2/etc.)
2. Double-click the .pkg file and follow the installation instructions
3. The application will be installed in your Applications folder

### Linux

For Debian/Ubuntu users:
1. Download the appropriate .deb package:
   - `BrowserRedirector-[version]-linux-x64.deb` - For 64-bit Intel/AMD systems (most common)
   - `BrowserRedirector-[version]-linux-arm.deb` - For 32-bit ARM systems (like Raspberry Pi)
   - `BrowserRedirector-[version]-linux-arm64.deb` - For 64-bit ARM systems
2. Install with your package manager or `sudo dpkg -i BrowserRedirector-*.deb`

For other Linux distributions:
1. Download the appropriate .deb package for your architecture
2. Install with `sudo dpkg -i BrowserRedirector-*.deb` or your distribution's package manager

## Usage

### Setting Up Browser Redirector

1. Launch Browser Redirector (without any command-line arguments)
2. The settings window will appear
3. Click "Add" to create a new URL pattern rule
4. Enter a name for the rule, a regex pattern, and select a browser
5. Click "Save Settings" to save your configuration
6. Click "Set as Default Browser" to configure your system to use Browser Redirector

### Pattern Examples

- `.*google\.com.*` - Matches any URL containing 'google.com'
- `.*\.pdf$` - Matches URLs ending with '.pdf'
- `^https://github\.com.*` - Matches URLs starting with 'https://github.com'

## Setting as Default Browser

### Windows

**Important:** You must install using the MSI installer to register Browser Redirector as a browser.

1. Click "Set as Default Browser"
2. Windows Settings will open
3. Find "Web browser" and select "Browser Redirector"

If "Browser Redirector" doesn't appear in the list, ensure you:
- Installed using the MSI installer (not the standalone executable)
- Selected the "Register as a browser option in Windows" option during installation

### macOS

1. Click "Set as Default Browser"
2. System Preferences will open
3. In "Default web browser", select "Browser Redirector"

### Linux

1. Click "Set as Default Browser"
2. If using a desktop environment like GNOME or KDE, the default applications settings will open
3. Select "Browser Redirector" as the default web browser
4. Alternatively, the application will attempt to set itself as the default using `xdg-settings`

## Building from Source

### Prerequisites

- .NET 9.0 SDK or later

### Build Instructions

To build for all platforms:

```bash
# Clone the repository
git clone https://github.com/your-username/browser-redirector.git
cd browser-redirector

# Make the build script executable
chmod +x build-all.sh

# Run the build script
./build-all.sh
```

The built applications will be in the `bin/Publish` directory and ZIP files in the `bin` directory.

## Release Process

### Creating a New Release

1. Update the version number in `BrowserRedirector.csproj`
2. Commit your changes
3. Create and push a new tag with the version number:

```bash
git tag v1.0.0  # Replace with actual version
git push origin v1.0.0
```

4. GitHub Actions will automatically:
   - Build the application for all platforms
   - Create Windows MSI installers and standalone executables
   - Create macOS .pkg installers for Intel and Apple Silicon
   - Create Linux .deb packages
   - Create a new GitHub release
   - Generate release notes from commit messages
   - Attach all build artifacts to the release

### Versioning

We follow [Semantic Versioning](https://semver.org/):
- `v1.0.0` - Full release
- `v1.0.0-pre` - Pre-release (automatically marked as pre-release on GitHub)
- `v1.0.0-rc1` - Release candidate

## License

[MIT License](LICENSE)