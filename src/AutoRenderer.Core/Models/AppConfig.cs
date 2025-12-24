namespace AutoRenderer.Core.Models;

public class AppConfig
{
    public string BlenderPath { get; set; } = string.Empty;
    public string DefaultOutputPath { get; set; } = string.Empty;
    public string LastUsedScenePath { get; set; } = string.Empty;
    
    // User preferences
    public bool AutoSave { get; set; } = true;
    public string Theme { get; set; } = "Dark";

    public VideoExportSettings VideoSettings { get; set; } = new VideoExportSettings();
}

public class VideoExportSettings
{
    public int FrameRate { get; set; } = 30;
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
    public string Codec { get; set; } = "H.264"; // H.264, H.265, VP9
    public string BitrateMode { get; set; } = "VBR"; // CBR, VBR
    public int BitrateKbps { get; set; } = 5000;
    public int GopSize { get; set; } = 18;
}
