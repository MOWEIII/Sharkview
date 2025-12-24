namespace AutoRenderer.Core.Models;

public class AppConfig
{
    public string BlenderPath { get; set; } = string.Empty;
    public string DefaultOutputPath { get; set; } = string.Empty;
    public string LastUsedScenePath { get; set; } = string.Empty;
    
    // User preferences
    public bool AutoSave { get; set; } = true;
    public string Theme { get; set; } = "Dark";
}
