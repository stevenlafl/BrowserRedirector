using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DefaultBrowser.Models
{
    public class UrlRedirector
    {
        private readonly AppSettings _settings;
        private readonly PlatformService _platformService;
        private readonly NotificationService _notificationService;

        public UrlRedirector(AppSettings settings)
        {
            _settings = settings;
            _platformService = PlatformService.Instance;
            _notificationService = NotificationService.Instance;
        }

        public bool ProcessUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Log.Warning("Empty URL provided to ProcessUrl");
                return false;
            }

            Log.Information("Processing URL: {Url}", url);

            // Check if any of the rules match
            var orderedMappings = _settings.BrowserMappings.OrderBy(m => m.Order).ToList();
            
            foreach (var mapping in orderedMappings)
            {
                if (mapping.MatchesUrl(url))
                {
                    Log.Information("URL matched pattern: {Pattern}, opening with browser: {Browser}", 
                        mapping.Pattern, mapping.BrowserPath);
                    
                    // Launch the matched browser
                    bool success = _platformService.LaunchBrowser(mapping.BrowserPath, url);
                    
                    if (success)
                    {
                        string browserName = Path.GetFileNameWithoutExtension(mapping.BrowserPath);
                        
                        // Show notification if enabled
                        if (_settings.ShowRedirectNotifications)
                        {
                            _notificationService.ShowNotification(
                                "URL Redirected", 
                                $"Opening {url} with {browserName} browser"
                            );
                        }
                    }
                    else
                    {
                        Log.Warning("Failed to launch browser {Browser} for URL {Url}", 
                            mapping.BrowserPath, url);
                        
                        // Try to fall back to default browser
                        if (!string.IsNullOrEmpty(_settings.DefaultBrowserPath))
                        {
                            Log.Information("Falling back to default browser: {Browser}", 
                                _settings.DefaultBrowserPath);
                            
                            success = _platformService.LaunchBrowser(_settings.DefaultBrowserPath, url);
                            
                            if (success)
                            {
                                string browserName = Path.GetFileNameWithoutExtension(_settings.DefaultBrowserPath);
                                
                                // Show notification if enabled
                                if (_settings.ShowRedirectNotifications)
                                {
                                    _notificationService.ShowNotification(
                                        "URL Redirected (Fallback)", 
                                        $"Opening {url} with {browserName} browser"
                                    );
                                }
                            }
                            else
                            {
                                Log.Warning("Failed to launch default browser {Browser}", 
                                    _settings.DefaultBrowserPath);
                                
                                // Try system default as last resort
                                bool defaultSuccess = _platformService.LaunchDefaultBrowser(url);
                                
                                if (defaultSuccess && _settings.ShowRedirectNotifications)
                                {
                                    _notificationService.ShowNotification(
                                        "URL Redirected (System Default)", 
                                        $"Opening {url} with system default browser"
                                    );
                                }
                                
                                return defaultSuccess;
                            }
                        }
                        else
                        {
                            // Try system default as last resort
                            Log.Information("No default browser set, using system default");
                            bool defaultSuccess = _platformService.LaunchDefaultBrowser(url);
                            
                            if (defaultSuccess && _settings.ShowRedirectNotifications)
                            {
                                _notificationService.ShowNotification(
                                    "URL Redirected (System Default)", 
                                    $"Opening {url} with system default browser"
                                );
                            }
                            
                            return defaultSuccess;
                        }
                    }
                    
                    return success;
                }
            }

            // If no rules match, use the default browser if specified
            if (!string.IsNullOrEmpty(_settings.DefaultBrowserPath))
            {
                Log.Information("No matching rules found, using default browser: {Browser}", 
                    _settings.DefaultBrowserPath);
                
                bool success = _platformService.LaunchBrowser(_settings.DefaultBrowserPath, url);
                
                if (success)
                {
                    string browserName = Path.GetFileNameWithoutExtension(_settings.DefaultBrowserPath);
                    
                    // Show notification if enabled
                    if (_settings.ShowRedirectNotifications)
                    {
                        _notificationService.ShowNotification(
                            "URL Redirected (Default)", 
                            $"Opening {url} with {browserName} browser"
                        );
                    }
                }
                else
                {
                    Log.Warning("Failed to launch default browser {Browser}, using system default", 
                        _settings.DefaultBrowserPath);
                    
                    // Try system default as last resort
                    bool defaultSuccess = _platformService.LaunchDefaultBrowser(url);
                    
                    if (defaultSuccess && _settings.ShowRedirectNotifications)
                    {
                        _notificationService.ShowNotification(
                            "URL Redirected (System Default)", 
                            $"Opening {url} with system default browser"
                        );
                    }
                    
                    return defaultSuccess;
                }
                
                return success;
            }
            else
            {
                // If no default is specified, use the system default browser
                Log.Information("No default browser specified, using system default");
                bool defaultSuccess = _platformService.LaunchDefaultBrowser(url);
                
                if (defaultSuccess && _settings.ShowRedirectNotifications)
                {
                    _notificationService.ShowNotification(
                        "URL Redirected (System Default)", 
                        $"Opening {url} with system default browser"
                    );
                }
                
                return defaultSuccess;
            }
        }
    }
}