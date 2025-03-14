using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace DefaultBrowser.Models
{
    public class PlatformService
    {
        private static readonly Lazy<PlatformService> _instance = new Lazy<PlatformService>(() => new PlatformService());
        public static PlatformService Instance => _instance.Value;

        private PlatformService() 
        {
            // Ensure settings directory exists
            var settingsDir = GetSettingsDirectory();
            if (!Directory.Exists(settingsDir))
            {
                try
                {
                    Directory.CreateDirectory(settingsDir);
                    Log.Information("Created settings directory: {Directory}", settingsDir);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to create settings directory: {Directory}", settingsDir);
                }
            }
        }

        public string GetSettingsDirectory()
        {
            string settingsDir;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                settingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "BrowserRedirector");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                settingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", "BrowserRedirector");
            }
            else // Linux and others
            {
                settingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config", "BrowserRedirector");
            }
            
            // Log the directory for debugging
            Log.Debug("Settings directory: {SettingsDir}", settingsDir);
            
            // Ensure the directory exists
            try
            {
                if (!Directory.Exists(settingsDir))
                {
                    Log.Information("Creating settings directory: {Directory}", settingsDir);
                    Directory.CreateDirectory(settingsDir);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create settings directory: {Directory}", settingsDir);
            }
            
            return settingsDir;
        }

        public string GetSettingsFilePath()
        {
            return Path.Combine(GetSettingsDirectory(), "settings.json");
        }

        public string GetLogFilePath()
        {
            return Path.Combine(GetSettingsDirectory(), "log.txt");
        }
        
        public async Task EnsureSettingsFileExistsAsync()
        {
            string settingsFilePath = GetSettingsFilePath();
            
            try
            {
                // Ensure directory exists first
                string? settingsDir = Path.GetDirectoryName(settingsFilePath);
                if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
                {
                    Log.Information("Creating settings directory: {Directory}", settingsDir);
                    Directory.CreateDirectory(settingsDir);
                }
                
                Log.Debug("Checking if settings file exists: {FilePath}", settingsFilePath);
                
                bool fileExists = false;
                try 
                {
                    fileExists = File.Exists(settingsFilePath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error checking if settings file exists: {FilePath}", settingsFilePath);
                }
                
                if (!fileExists)
                {
                    Log.Information("Creating default settings file at: {FilePath}", settingsFilePath);
                    
                    // Create a default settings object
                    var defaultSettings = new AppSettings
                    {
                        LogRedirects = true,
                        BrowserMappings = new List<BrowserMapping>()
                    };
                    
                    try
                    {
                        // Try to save using File.WriteAllText directly first with source generator
                        string json = System.Text.Json.JsonSerializer.Serialize(defaultSettings, AppSettingsContext.Default.AppSettings);
                        
                        File.WriteAllText(settingsFilePath, json);
                        Log.Information("Default settings file created successfully using direct write");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to write settings file directly, trying SaveAsync method");
                        
                        // Try the SaveAsync method as a backup
                        await defaultSettings.SaveAsync(settingsFilePath);
                        Log.Information("Default settings file created successfully using SaveAsync");
                    }
                }
                else
                {
                    Log.Debug("Settings file already exists: {FilePath}", settingsFilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to ensure settings file exists: {FilePath}", settingsFilePath);
            }
        }

        public bool SetAsDefaultBrowser()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return SetAsDefaultBrowserWindows();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return SetAsDefaultBrowserMacOS();
                }
                else // Linux and others
                {
                    return SetAsDefaultBrowserLinux();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SetAsDefaultBrowserWindows()
        {
            // On Windows, we need to show instructions to the user
            // as setting the default browser often requires manual action
            
            // Launch Windows default apps settings
            var psi = new ProcessStartInfo
            {
                FileName = "ms-settings:defaultapps",
                UseShellExecute = true
            };
            
            Process.Start(psi);
            return true; // Return true to indicate we opened the settings page
        }

        private bool SetAsDefaultBrowserMacOS()
        {
            // On macOS, open System Preferences and show instructions to the user
            var psi = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = "/System/Applications/System\\ Settings.app",
                UseShellExecute = true
            };
            
            Process.Start(psi);
            return true; // Return true to indicate we opened the settings page
        }

        private bool SetAsDefaultBrowserLinux()
        {
            // Try to run xdg-settings command
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                    return false;

                // Create or update .desktop file
                string desktopFileContents = $@"[Desktop Entry]
Type=Application
Name=Browser Redirector
Comment=Browser URL Redirector
Exec={exePath} %u
Icon=web-browser
MimeType=x-scheme-handler/http;x-scheme-handler/https;
Categories=Network;WebBrowser;
";

                string desktopFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share", "applications", "browser-redirector.desktop");

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(desktopFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(desktopFilePath, desktopFileContents);

                // Make it executable
                var psi = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{desktopFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit();

                // Set as default browser
                psi = new ProcessStartInfo
                {
                    FileName = "xdg-settings",
                    Arguments = "set default-web-browser browser-redirector.desktop",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<InstalledBrowser> GetInstalledBrowsers()
        {
            var browsers = new List<InstalledBrowser>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows browser detection (simplified)
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                // Check for common browsers
                TryAddBrowser(browsers, "Google Chrome", Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"));
                TryAddBrowser(browsers, "Google Chrome (x86)", Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"));
                TryAddBrowser(browsers, "Mozilla Firefox", Path.Combine(programFiles, "Mozilla Firefox", "firefox.exe"));
                TryAddBrowser(browsers, "Mozilla Firefox (x86)", Path.Combine(programFilesX86, "Mozilla Firefox", "firefox.exe"));
                TryAddBrowser(browsers, "Microsoft Edge", Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"));
                TryAddBrowser(browsers, "Microsoft Edge (x86)", Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"));
                TryAddBrowser(browsers, "Brave", Path.Combine(programFiles, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
                TryAddBrowser(browsers, "Brave (x86)", Path.Combine(programFilesX86, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS browser detection
                TryAddBrowser(browsers, "Safari", "/Applications/Safari.app");
                TryAddBrowser(browsers, "Google Chrome", "/Applications/Google Chrome.app");
                TryAddBrowser(browsers, "Mozilla Firefox", "/Applications/Firefox.app");
                TryAddBrowser(browsers, "Microsoft Edge", "/Applications/Microsoft Edge.app");
                TryAddBrowser(browsers, "Brave", "/Applications/Brave Browser.app");
            }
            else // Linux and others
            {
                // Linux browser detection using 'which' command
                TryAddLinuxBrowser(browsers, "Firefox", "firefox");
                TryAddLinuxBrowser(browsers, "Google Chrome", "google-chrome");
                TryAddLinuxBrowser(browsers, "Google Chrome", "google-chrome-stable");
                TryAddLinuxBrowser(browsers, "Chromium", "chromium");
                TryAddLinuxBrowser(browsers, "Chromium", "chromium-browser");
                TryAddLinuxBrowser(browsers, "Brave", "brave-browser");
                TryAddLinuxBrowser(browsers, "Microsoft Edge", "microsoft-edge");
            }

            return browsers;
        }

        private void TryAddBrowser(List<InstalledBrowser> browsers, string name, string path)
        {
            if (File.Exists(path))
            {
                browsers.Add(new InstalledBrowser { Name = name, Path = path });
            }
        }

        private void TryAddLinuxBrowser(List<InstalledBrowser> browsers, string name, string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output) && File.Exists(output))
                    {
                        browsers.Add(new InstalledBrowser { Name = name, Path = output });
                    }
                }
            }
            catch
            {
                // Ignore errors during browser detection
            }
        }

        public bool LaunchBrowser(string browserPath, string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = browserPath,
                        Arguments = url,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // On macOS, we use 'open' command
                    var psi = new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"-a \"{browserPath}\" \"{url}\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return true;
                }
                else // Linux and others
                {
                    // Just try to launch the browser directly with the URL
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = browserPath,
                            Arguments = $"\"{url}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        var process = Process.Start(psi);
                        if (process != null)
                        {
                            // Wait a short time to make sure the process started
                            if (!process.WaitForExit(500))
                            {
                                // Process is still running, which is good
                                return true;
                            }
                            
                            // If process exited quickly with zero code, it might have worked
                            if (process.ExitCode == 0)
                            {
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to launch browser {Browser} directly, trying alternative methods", browserPath);
                    }
                    
                    // If direct launch fails, try a different approach by falling back to default browser
                    return LaunchDefaultBrowser(url);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error launching browser {Browser} with URL: {Url}", browserPath, url);
                return false;
            }
        }

        public string GetFriendlyBrowserName(string browserPath)
        {
            if (string.IsNullOrEmpty(browserPath))
                return "No browser selected";
                
            try
            {
                // First, check if this matches a known browser in our installed list
                foreach (var browser in GetInstalledBrowsers())
                {
                    if (browser.Path.Equals(browserPath, StringComparison.OrdinalIgnoreCase))
                        return browser.Name;
                }
                
                // Get filename without extension
                string fileName = Path.GetFileNameWithoutExtension(browserPath);
                
                // Common browser names to make more user-friendly
                Dictionary<string, string> commonBrowsers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "chrome", "Google Chrome" },
                    { "firefox", "Mozilla Firefox" },
                    { "msedge", "Microsoft Edge" },
                    { "safari", "Safari" },
                    { "opera", "Opera" },
                    { "brave", "Brave" },
                    { "vivaldi", "Vivaldi" },
                    { "iexplore", "Internet Explorer" }
                };
                
                // If it's a known browser, return its friendly name
                if (commonBrowsers.TryGetValue(fileName, out string? friendlyName))
                    return friendlyName;
                
                // Try to find a match that contains the filename
                foreach (var browser in commonBrowsers)
                {
                    if (fileName.Contains(browser.Key, StringComparison.OrdinalIgnoreCase))
                        return browser.Value;
                }
                
                // Capital case the filename (e.g., "firefox" -> "Firefox")
                if (!string.IsNullOrEmpty(fileName) && fileName.Length > 1)
                    return char.ToUpper(fileName[0]) + fileName.Substring(1);
                
                // Fallback: just return the filename
                return fileName;
            }
            catch
            {
                // If there's any error, return the raw path
                return Path.GetFileName(browserPath);
            }
        }
        
        public bool LaunchDefaultBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On Windows, we can use Process.Start with the URL directly
                    var psi = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // On macOS, we use 'open' command
                    var psi = new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{url}\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    return true;
                }
                else // Linux and others
                {
                    // Try multiple approaches for Linux
                    
                    // Try using a common browser directly
                    string[] commonBrowsers = { "firefox", "google-chrome", "chromium", "chromium-browser", "mozilla" };
                    foreach (var browser in commonBrowsers)
                    {
                        try
                        {
                            var checkPsi = new ProcessStartInfo
                            {
                                FileName = "which",
                                Arguments = browser,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            };
                            
                            using var checkProcess = Process.Start(checkPsi);
                            if (checkProcess != null)
                            {
                                string output = checkProcess.StandardOutput.ReadToEnd().Trim();
                                checkProcess.WaitForExit();
                                
                                if (!string.IsNullOrEmpty(output) && checkProcess.ExitCode == 0)
                                {
                                    // Browser found, try to use it
                                    var browserPsi = new ProcessStartInfo
                                    {
                                        FileName = output,
                                        Arguments = $"\"{url}\"",
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    };
                                    
                                    Process.Start(browserPsi);
                                    return true;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore and try the next browser
                        }
                    }

                    // Try different opener commands
                    string[] openerCommands = { "xdg-open", "sensible-browser", "gnome-open", "kde-open" };
                    foreach (var opener in openerCommands)
                    {
                        try
                        {
                            var checkPsi = new ProcessStartInfo
                            {
                                FileName = "which",
                                Arguments = opener,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            };
                            
                            using var checkProcess = Process.Start(checkPsi);
                            if (checkProcess != null)
                            {
                                string output = checkProcess.StandardOutput.ReadToEnd().Trim();
                                checkProcess.WaitForExit();
                                
                                if (!string.IsNullOrEmpty(output) && checkProcess.ExitCode == 0)
                                {
                                    // Opener found, try to use it
                                    var openerPsi = new ProcessStartInfo
                                    {
                                        FileName = output,
                                        Arguments = $"\"{url}\"",
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    };
                                    
                                    Process.Start(openerPsi);
                                    return true;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore and try the next opener
                        }
                    }
                    
                    // Last resort: try Python's webbrowser module
                    try
                    {
                        var pythonPsi = new ProcessStartInfo
                        {
                            FileName = "python3",
                            Arguments = $"-c \"import webbrowser; webbrowser.open('{url}')\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        Process.Start(pythonPsi);
                        return true;
                    }
                    catch
                    {
                        // All attempts failed
                        Log.Error("Failed to open URL: No suitable browser or opener found on the system");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error launching default browser with URL: {Url}", url);
                return false;
            }
        }
    }
}