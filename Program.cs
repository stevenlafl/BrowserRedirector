using Avalonia;
using DefaultBrowser.Models;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DefaultBrowser;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Set up Serilog early on for command-line mode
        var platformService = PlatformService.Instance;
        
        // Create log directory if it doesn't exist
        var logDirectory = Path.GetDirectoryName(platformService.GetLogFilePath());
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
        
        // Configure Serilog with console logging and file logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug() // Set to Debug to capture more information
            .WriteTo.File(platformService.GetLogFilePath(), rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();
            
        // Log basic system information
        Log.Information("System: {OS} / {Framework}", 
            Environment.OSVersion.ToString(), 
            Environment.Version.ToString());
            
        Log.Information("Application started with {ArgCount} arguments", args.Length);
        
        // Check if we have arguments (URL to open)
        if (args.Length > 0)
        {
            // We're being called to open a URL
            ProcessUrl(args).Wait();
        }
        else
        {
            // No arguments, open the settings UI
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }
    
    private static async Task ProcessUrl(string[] args)
    {
        try
        {
            Log.Information("Processing URL in command-line mode");
            
            // Extract URL from arguments
            string url = ExtractUrl(args);
            
            if (string.IsNullOrEmpty(url))
            {
                Log.Warning("No valid URL found in arguments");
                return;
            }
            
            Log.Information("URL to process: {Url}", url);
            
            // Load settings
            var platformService = PlatformService.Instance;
            var settings = await AppSettings.LoadAsync(platformService.GetSettingsFilePath());
            
            // Create URL redirector
            var redirector = new UrlRedirector(settings);
            
            // Process the URL
            bool success = redirector.ProcessUrl(url);
            
            if (success)
            {
                Log.Information("URL processed successfully");
            }
            else
            {
                Log.Warning("Failed to process URL");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing URL");
        }
        finally
        {
            // Ensure logs are flushed
            Log.CloseAndFlush();
        }
    }
    
    private static string ExtractUrl(string[] args)
    {
        // Common URL protocols
        string[] protocols = new[] { "http://", "https://", "ftp://", "file://", "mailto:" };
        
        // First, try to find a complete URL in the arguments
        foreach (var arg in args)
        {
            if (protocols.Any(p => arg.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return arg;
            }
        }
        
        // If we couldn't find a fully qualified URL, just return the first argument
        // as it might be a partial URL or a different format
        return args.Length > 0 ? args[0] : string.Empty;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
