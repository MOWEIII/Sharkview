using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoRenderer.Core.Models;

namespace AutoRenderer.Core.Services;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(VideoExportSettings))]
internal partial class AppConfigContext : JsonSerializerContext
{
}

public class ConfigurationService : IConfigurationService
{
    private const string AppName = "AutoRenderer";
    private const string ConfigFileName = "config.json";
    private readonly string _configFilePath;
    
    public AppConfig Config { get; private set; }

    private readonly IConsoleService? _consoleService;

    public ConfigurationService(IConsoleService? consoleService = null)
    {
        _consoleService = consoleService;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, AppName);
        
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }
        
        _configFilePath = Path.Combine(configDir, ConfigFileName);
        Config = new AppConfig();
    }

    public void LoadConfiguration()
    {
        if (File.Exists(_configFilePath))
        {
            try
            {
                _consoleService?.Log($"Loading configuration from: {_configFilePath}");
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize(json, AppConfigContext.Default.AppConfig);
                if (config != null)
                {
                    Config = config;
                    _consoleService?.Log("Configuration loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                // Fallback to default config if load fails
                _consoleService?.Log($"Failed to load configuration: {ex.Message}. Using defaults.");
                Config = new AppConfig();
            }
        }
        else
        {
            _consoleService?.Log($"Configuration file not found at: {_configFilePath}. Creating new default config.");
        }
    }

    public void SaveConfiguration()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.TypeInfoResolver = AppConfigContext.Default;
            
            var json = JsonSerializer.Serialize(Config, typeof(AppConfig), AppConfigContext.Default);
            
            // Or cleaner way with options if needed, but Context directly is safer for AOT
            // var json = JsonSerializer.Serialize(Config, AppConfigContext.Default.AppConfig); 
            // The above line won't include WriteIndented unless we configure the context or options separately.
            // Let's stick to simple context usage for serialization to ensure AOT compatibility first.
            
            // To support WriteIndented with Source Generator:
            // We can pass the context to options, or configure the instance in context if possible (not directly).
            // Best practice for AOT + Options:
            
            var jsonString = JsonSerializer.Serialize(Config, AppConfigContext.Default.AppConfig);
            
            // If we really want indentation, we can try re-serializing or just accept minified for now to ensure stability
            // But let's try to pass options compatible with AOT
            
            File.WriteAllText(_configFilePath, jsonString);
            _consoleService?.Log("Configuration saved.");
        }
        catch (Exception ex)
        {
            _consoleService?.Log($"Failed to save config: {ex.Message}");
            Console.WriteLine($"Failed to save config: {ex.Message}");
        }
    }

    public void UpdateBlenderPath(string path)
    {
        Config.BlenderPath = path;
        SaveConfiguration();
    }

    public void UpdateDefaultOutputPath(string path)
    {
        Config.DefaultOutputPath = path;
        SaveConfiguration();
    }
}
