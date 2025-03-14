using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Serilog;

namespace DefaultBrowser.Models
{
    [JsonSerializable(typeof(AppSettings))]
    [JsonSerializable(typeof(BrowserMapping))]
    [JsonSerializable(typeof(List<BrowserMapping>))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class AppSettingsContext : JsonSerializerContext {}
    
    public class AppSettings
    {
        public List<BrowserMapping> BrowserMappings { get; set; } = new List<BrowserMapping>();
        public string DefaultBrowserPath { get; set; } = string.Empty;
        public bool LogRedirects { get; set; } = false;
        public bool ShowRedirectNotifications { get; set; } = false;

        public static async Task<AppSettings> LoadAsync(string filePath)
        {
            try
            {
                Log.Information("Attempting to load settings from: {FilePath}", filePath);
                
                if (File.Exists(filePath))
                {
                    Log.Information("Settings file exists, reading content");
                    string json = await File.ReadAllTextAsync(filePath);
                    
                    // Use source-generated serializer
                    var settings = JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings);
                    
                    if (settings != null)
                    {
                        Log.Information("Successfully loaded settings. Default browser: {DefaultBrowser}, Rule count: {RuleCount}", 
                            settings.DefaultBrowserPath, settings.BrowserMappings.Count);
                        return settings;
                    }
                    else
                    {
                        Log.Warning("Settings file deserialized to null");
                    }
                }
                else
                {
                    Log.Information("Settings file does not exist: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading settings from {FilePath}", filePath);
            }

            Log.Information("Using default settings");
            return new AppSettings();
        }

        public async Task SaveAsync(string filePath)
        {
            try
            {
                Log.Information("Saving settings to: {FilePath}", filePath);
                Log.Information("Settings to save - Default browser: {DefaultBrowser}, Rule count: {RuleCount}", 
                    DefaultBrowserPath, BrowserMappings.Count);
                
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Log.Information("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }
                
                // Use source-generated serializer
                string json = JsonSerializer.Serialize(this, AppSettingsContext.Default.AppSettings);
                Log.Debug("Settings JSON: {Json}", json);
                
                await File.WriteAllTextAsync(filePath, json);
                // Logging for success is done at a higher level
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving settings to {FilePath}", filePath);
            }
        }
    }
}