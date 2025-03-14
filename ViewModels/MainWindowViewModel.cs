using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DefaultBrowser.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DefaultBrowser.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PlatformService _platformService;
    private readonly string _settingsFilePath;
    private AppSettings _settings = new AppSettings();
    // This is auto-implemented by the [ObservableProperty] attribute
    //private BrowserMapping? _selectedMapping;
    
    public ObservableCollection<BrowserMapping> BrowserMappings { get; } = new();
    public AvaloniaList<InstalledBrowser> InstalledBrowsers { get; } = new();
    
    private BrowserMapping? _selectedMapping;
    private InstalledBrowser? _selectedBrowser;
    
    public BrowserMapping? SelectedMapping
    {
        get => _selectedMapping;
        set
        {
            if (SetProperty(ref _selectedMapping, value))
            {
                // If browser path changes, update selected dropdown item
                if (_selectedMapping != null)
                {
                    // Set the selectedBrowser property when the mapping changes
                    UpdateSelectedBrowserFromPath(_selectedMapping.BrowserPath);
                }
            }
        }
    }
    
    [ObservableProperty]
    private bool showMappingBrowseButton;
    
    public InstalledBrowser? SelectedBrowser
    {
        get => _selectedBrowser;
        set 
        {
            if (SetProperty(ref _selectedBrowser, value))
            {
                if (value != null)
                {
                    // Check if this is the "Other" option
                    ShowMappingBrowseButton = value.Path == "OTHER_BROWSER";
                    
                    // Update the BrowserPath when the dropdown selection changes
                    if (SelectedMapping != null && value.Path != "OTHER_BROWSER")
                    {
                        SelectedMapping.BrowserPath = value.Path;
                    }
                }
                else
                {
                    ShowMappingBrowseButton = false;
                }
            }
        }
    }
    
    private string _defaultBrowserPath = string.Empty;
    private InstalledBrowser? _selectedDefaultBrowser;
    
    public string DefaultBrowserPath
    {
        get => _defaultBrowserPath;
        set
        {
            if (SetProperty(ref _defaultBrowserPath, value))
            {
                // Update selected default browser when path changes
                UpdateSelectedDefaultBrowserFromPath(value);
            }
        }
    }
    
    [ObservableProperty]
    private bool showDefaultBrowseButton;
    
    public InstalledBrowser? SelectedDefaultBrowser
    {
        get => _selectedDefaultBrowser;
        set
        {
            if (SetProperty(ref _selectedDefaultBrowser, value))
            {
                if (value != null)
                {
                    // Check if this is the "Other" option
                    ShowDefaultBrowseButton = value.Path == "OTHER_BROWSER";
                    
                    // Update the DefaultBrowserPath when the dropdown selection changes
                    if (value.Path != "OTHER_BROWSER")
                    {
                        DefaultBrowserPath = value.Path;
                    }
                }
                else
                {
                    ShowDefaultBrowseButton = false;
                }
            }
        }
    }
    
    [ObservableProperty]
    private bool logRedirects = false;
    
    [ObservableProperty]
    private bool showRedirectNotifications = false;
    
    private void UpdateSelectedBrowserFromPath(string browserPath)
    {
        if (string.IsNullOrEmpty(browserPath))
        {
            SelectedBrowser = null;
            return;
        }
        
        // Find the browser in InstalledBrowsers that matches the path
        var matchingBrowser = InstalledBrowsers.FirstOrDefault(b => b.Path == browserPath);
        
        // If not found in our list but the path exists, select "Other"
        if (matchingBrowser == null && browserPath != "OTHER_BROWSER")
        {
            matchingBrowser = InstalledBrowsers.FirstOrDefault(b => b.Path == "OTHER_BROWSER");
        }
        
        SelectedBrowser = matchingBrowser;
    }
    
    private void UpdateSelectedDefaultBrowserFromPath(string browserPath)
    {
        if (string.IsNullOrEmpty(browserPath))
        {
            SelectedDefaultBrowser = null;
            return;
        }
        
        // Find the browser in InstalledBrowsers that matches the path
        var matchingBrowser = InstalledBrowsers.FirstOrDefault(b => b.Path == browserPath);
        
        // If not found in our list but the path exists, select "Other"
        if (matchingBrowser == null && browserPath != "OTHER_BROWSER")
        {
            matchingBrowser = InstalledBrowsers.FirstOrDefault(b => b.Path == "OTHER_BROWSER");
        }
        
        SelectedDefaultBrowser = matchingBrowser;
    }
    
    [ObservableProperty]
    private string statusMessage = string.Empty;
    
    public MainWindowViewModel()
    {
        _platformService = PlatformService.Instance;
        _settingsFilePath = _platformService.GetSettingsFilePath();
        
        // Initialize browse button visibility (hidden by default)
        ShowMappingBrowseButton = false;
        ShowDefaultBrowseButton = false;
        
        // Initialize logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(_platformService.GetLogFilePath(), rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();
            
        Log.Information("Application started");
        
        // Load settings asynchronously
        Task.Run(async () =>
        {
            // Ensure settings file exists
            await _platformService.EnsureSettingsFileExistsAsync();
            
            // Load settings and browsers
            await LoadSettingsAsync();
            await LoadInstalledBrowsersAsync();
        });
    }
    
    private async Task LoadSettingsAsync()
    {
        try 
        {
            _settings = await AppSettings.LoadAsync(_settingsFilePath);
            
            // Update observable collections
            BrowserMappings.Clear();
            foreach (var mapping in _settings.BrowserMappings.OrderBy(m => m.Order))
            {
                BrowserMappings.Add(mapping);
            }
            
            DefaultBrowserPath = _settings.DefaultBrowserPath;
            LogRedirects = _settings.LogRedirects;
            ShowRedirectNotifications = _settings.ShowRedirectNotifications;
            
            // If we have any mappings, select the first one
            if (BrowserMappings.Count > 0)
            {
                SelectedMapping = BrowserMappings[0];
            }
            
            // Update dropdowns based on paths
            if (SelectedMapping != null)
            {
                UpdateSelectedBrowserFromPath(SelectedMapping.BrowserPath);
            }
            
            UpdateSelectedDefaultBrowserFromPath(DefaultBrowserPath);
            
            StatusMessage = "Settings loaded successfully";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading settings");
            StatusMessage = "Error loading settings";
            
            _settings = new AppSettings();
        }
    }
    
    private Task LoadInstalledBrowsersAsync()
    {
        try
        {
            var browsers = _platformService.GetInstalledBrowsers();
            
            InstalledBrowsers.Clear();
            
            // Add all detected browsers
            foreach (var browser in browsers)
            {
                InstalledBrowsers.Add(browser);
            }
            
            // Add "Other (not listed)" option
            InstalledBrowsers.Add(new InstalledBrowser { 
                Name = "Other (not listed)", 
                Path = "OTHER_BROWSER" 
            });
            
            // Set selected browsers based on paths
            if (!string.IsNullOrEmpty(DefaultBrowserPath))
            {
                UpdateSelectedDefaultBrowserFromPath(DefaultBrowserPath);
            }
            
            if (SelectedMapping != null && !string.IsNullOrEmpty(SelectedMapping.BrowserPath))
            {
                UpdateSelectedBrowserFromPath(SelectedMapping.BrowserPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error detecting installed browsers");
            StatusMessage = "Error detecting browsers";
        }
        
        return Task.CompletedTask;
    }
    
    [RelayCommand]
    internal async Task SaveSettingsAsync()
    {
        try
        {
            Log.Information("SaveSettingsAsync called. Saving settings to: {FilePath}", _settingsFilePath);
            
            // Make sure the directory exists
            string? settingsDir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
            {
                Log.Information("Creating settings directory: {Directory}", settingsDir);
                Directory.CreateDirectory(settingsDir);
            }
            
            // Update ordered index before saving
            int order = 0;
            foreach (var mapping in BrowserMappings)
            {
                mapping.Order = order++;
            }
            
            // Log what we're about to save
            Log.Information("Saving settings - DefaultBrowserPath: {Path}, MappingCount: {Count}", 
                DefaultBrowserPath, BrowserMappings.Count);
            
            // Update settings from UI
            _settings.BrowserMappings = BrowserMappings.ToList();
            _settings.DefaultBrowserPath = DefaultBrowserPath;
            _settings.LogRedirects = LogRedirects;
            _settings.ShowRedirectNotifications = ShowRedirectNotifications;
            
            try
            {
                // Save settings
                await _settings.SaveAsync(_settingsFilePath);
                
                // Verify file was saved
                if (File.Exists(_settingsFilePath))
                {
                    string json = await File.ReadAllTextAsync(_settingsFilePath);
                    Log.Debug("Settings file content: {Json}", json);
                    
                    Log.Information("ViewModel: Settings saved and verified successfully at {FilePath}", _settingsFilePath);
                    StatusMessage = "Settings saved successfully";
                }
                else
                {
                    // File doesn't exist, try a direct save approach
                    Log.Warning("Settings file not found after save, trying direct write approach");
                    
                    string json = System.Text.Json.JsonSerializer.Serialize(_settings, AppSettingsContext.Default.AppSettings);
                    
                    File.WriteAllText(_settingsFilePath, json);
                    
                    // Verify the file was saved
                    if (File.Exists(_settingsFilePath))
                    {
                        Log.Information("Settings saved successfully via direct write");
                        StatusMessage = "Settings saved successfully";
                    }
                    else
                    {
                        throw new Exception("Failed to save settings file via both methods");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during settings save operation");
                
                // Try one more time with synchronous File.WriteAllText
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(_settings, AppSettingsContext.Default.AppSettings);
                    
                    File.WriteAllText(_settingsFilePath, json);
                    Log.Information("Settings saved successfully via fallback write");
                    StatusMessage = "Settings saved successfully";
                }
                catch (Exception innerEx)
                {
                    Log.Error(innerEx, "Final attempt to save settings failed");
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving settings to {FilePath}", _settingsFilePath);
            StatusMessage = "Error saving settings";
        }
    }
    
    [RelayCommand]
    internal void AddMapping()
    {
        var newMapping = new BrowserMapping
        {
            Name = "New Rule",
            Pattern = ".*example\\.com.*",
            Order = BrowserMappings.Count
        };
        
        BrowserMappings.Add(newMapping);
        SelectedMapping = newMapping;
    }
    
    [RelayCommand]
    internal void RemoveMapping()
    {
        if (SelectedMapping != null)
        {
            BrowserMappings.Remove(SelectedMapping);
            SelectedMapping = BrowserMappings.FirstOrDefault();
        }
    }
    
    [RelayCommand]
    internal void MoveUp()
    {
        if (SelectedMapping != null)
        {
            int index = BrowserMappings.IndexOf(SelectedMapping);
            if (index > 0)
            {
                BrowserMappings.Move(index, index - 1);
            }
        }
    }
    
    [RelayCommand]
    internal void MoveDown()
    {
        if (SelectedMapping != null)
        {
            int index = BrowserMappings.IndexOf(SelectedMapping);
            if (index < BrowserMappings.Count - 1)
            {
                BrowserMappings.Move(index, index + 1);
            }
        }
    }
    
    // This property will be set by the view
    public TopLevel? CurrentTopLevel { get; set; }

    [RelayCommand]
    internal async Task BrowseForBrowserAsync(string target)
    {
        try
        {
            Log.Information("BrowseForBrowserAsync called with target: {Target}", target);
            
            // Try to get the active window as a safer approach
            var activeWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : CurrentTopLevel;
            
            if (activeWindow != null)
            {
                Log.Information("Got active window, showing file picker");
                
                var options = new FilePickerOpenOptions
                {
                    Title = "Select Browser",
                    AllowMultiple = false
                };

                var files = await activeWindow.StorageProvider.OpenFilePickerAsync(options);
                
                if (files.Count > 0)
                {
                    var selectedPath = files[0].Path.LocalPath;
                    Log.Information("Selected file: {Path}", selectedPath);
                    
                    if (target == "default")
                    {
                        DefaultBrowserPath = selectedPath;
                        Log.Information("Updated DefaultBrowserPath to: {Path}", selectedPath);
                        
                        // Keep "Other" selected if that's what was chosen
                        if (SelectedDefaultBrowser?.Path == "OTHER_BROWSER")
                        {
                            ShowDefaultBrowseButton = true;
                        }
                    }
                    else if (target == "mapping" && SelectedMapping != null)
                    {
                        SelectedMapping.BrowserPath = selectedPath;
                        Log.Information("Updated SelectedMapping.BrowserPath to: {Path}", selectedPath);
                        
                        // Keep "Other" selected if that's what was chosen
                        if (SelectedBrowser?.Path == "OTHER_BROWSER")
                        {
                            ShowMappingBrowseButton = true;
                        }
                    }
                }
                else
                {
                    Log.Information("No file was selected");
                }
            }
            else
            {
                Log.Warning("Unable to get active window or TopLevel for file picker");
                StatusMessage = "Unable to open file browser";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in BrowseForBrowserAsync");
            StatusMessage = "Error opening file browser";
        }
    }
    
    [RelayCommand]
    internal void SetAsDefaultBrowser()
    {
        try
        {
            bool success = _platformService.SetAsDefaultBrowser();
            if (success)
            {
                StatusMessage = "Default browser settings opened. Please set this app as your default browser.";
            }
            else
            {
                StatusMessage = "Failed to set as default browser";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting as default browser");
            StatusMessage = "Error setting as default browser";
        }
    }
    
    [RelayCommand]
    internal void ValidatePattern()
    {
        if (SelectedMapping != null)
        {
            try
            {
                // Try to create a regex from the pattern to validate it
                var regex = new Regex(SelectedMapping.Pattern, RegexOptions.IgnoreCase);
                StatusMessage = "Pattern is valid";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Invalid pattern: {ex.Message}";
            }
        }
    }
}
