using System;
using System.IO;
using System.Text.Json;
using AutoRenderer.Core.Models;

namespace AutoRenderer.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private const string AppName = "AutoRenderer";
    private const string ConfigFileName = "config.json";
    private readonly string _configFilePath;
    
    public AppConfig Config { get; private set; }

    public ConfigurationService()
    {
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
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    Config = config;
                }
            }
            catch (Exception)
            {
                // Fallback to default config if load fails
                Config = new AppConfig();
            }
        }
    }

    public void SaveConfiguration()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(Config, options);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            // TODO: Log error
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
