using System.Threading.Tasks;
using AutoRenderer.Core.Services;
using AutoRenderer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoRenderer.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IBlenderService _blenderService;
    private readonly IFilePickerService _filePickerService;

    [ObservableProperty]
    private string _blenderPath;

    [ObservableProperty]
    private string _defaultOutputPath;

    [ObservableProperty]
    private string _statusMessage = "";

    // Video Settings
    [ObservableProperty] private int _frameRate;
    [ObservableProperty] private int _resolutionWidth;
    [ObservableProperty] private int _resolutionHeight;
    [ObservableProperty] private string _codec;
    [ObservableProperty] private string _bitrateMode;
    [ObservableProperty] private int _bitrateKbps;
    [ObservableProperty] private int _gopSize;
    [ObservableProperty] private string _renderEngine;
    [ObservableProperty] private string _outputFileName = "output";
    [ObservableProperty] private double _duration = 5.0;
    [ObservableProperty] private string _selectedFormat = "MP4";

    public System.Collections.Generic.List<int> AvailableFrameRates { get; } = new() { 24, 30, 60 };
    public System.Collections.Generic.List<string> AvailableCodecs { get; } = new() { "H.264", "H.265", "VP9" };
    public System.Collections.Generic.List<string> AvailableBitrateModes { get; } = new() { "CBR", "VBR" };
    public System.Collections.Generic.List<string> AvailableRenderEngines { get; } = new() { "BLENDER_EEVEE", "CYCLES" };
    public System.Collections.Generic.List<string> AvailableResolutions { get; } = new() { "1280x720", "1920x1080", "3840x2160", "Custom" };
    public System.Collections.Generic.List<string> AvailableFormats { get; } = new() { "MP4", "AVI", "MKV" };

    [ObservableProperty]
    private string _selectedResolutionPreset;

    partial void OnSelectedResolutionPresetChanged(string value)
    {
        if (value == "1280x720") { ResolutionWidth = 1280; ResolutionHeight = 720; }
        else if (value == "1920x1080") { ResolutionWidth = 1920; ResolutionHeight = 1080; }
        else if (value == "3840x2160") { ResolutionWidth = 3840; ResolutionHeight = 2160; }
    }

    public SettingsViewModel(
        IConfigurationService configService, 
        IBlenderService blenderService,
        IFilePickerService filePickerService)
    {
        _configService = configService;
        _blenderService = blenderService;
        _filePickerService = filePickerService;

        _blenderPath = _configService.Config.BlenderPath;
        _defaultOutputPath = _configService.Config.DefaultOutputPath;

        // Load Video Settings
        var v = _configService.Config.VideoSettings;
        _frameRate = v.FrameRate;
        _resolutionWidth = v.ResolutionWidth;
        _resolutionHeight = v.ResolutionHeight;
        _codec = v.Codec;
        _bitrateMode = v.BitrateMode;
        _bitrateKbps = v.BitrateKbps;
        _gopSize = v.GopSize;
        _renderEngine = v.RenderEngine;
        _outputFileName = v.OutputName;
        _duration = v.Duration;
        if (string.IsNullOrEmpty(_renderEngine)) _renderEngine = "BLENDER_EEVEE";

        _selectedResolutionPreset = $"{_resolutionWidth}x{_resolutionHeight}";
        if (!AvailableResolutions.Contains(_selectedResolutionPreset)) _selectedResolutionPreset = "Custom";
    }

    [RelayCommand]
    private async Task AutoDetectBlender()
    {
        StatusMessage = "Detecting Blender...";
        var path = await _blenderService.AutoDetectBlenderPathAsync();
        if (!string.IsNullOrEmpty(path))
        {
            BlenderPath = path;
            _configService.UpdateBlenderPath(path);
            StatusMessage = "Blender detected successfully.";
        }
        else
        {
            StatusMessage = "Blender not found automatically.";
        }
    }

    [RelayCommand]
    private async Task BrowseBlenderPath()
    {
        var path = await _filePickerService.PickFileAsync("Select Blender Executable", new[] { "*.exe", "blender" });
        if (!string.IsNullOrEmpty(path))
        {
            if (await _blenderService.ValidateBlenderPathAsync(path))
            {
                BlenderPath = path;
                _configService.UpdateBlenderPath(path);
                StatusMessage = "Blender path updated.";
            }
            else
            {
                StatusMessage = "Invalid Blender executable selected.";
            }
        }
    }

    [RelayCommand]
    private async Task BrowseOutputPath()
    {
        var path = await _filePickerService.PickFolderAsync("Select Default Output Directory");
        if (!string.IsNullOrEmpty(path))
        {
            DefaultOutputPath = path;
            _configService.UpdateDefaultOutputPath(path);
            StatusMessage = "Output path updated.";
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        // Save Core Settings
        _configService.Config.BlenderPath = BlenderPath;
        _configService.Config.DefaultOutputPath = DefaultOutputPath;

        // Save Video Settings
        var v = _configService.Config.VideoSettings;
        v.FrameRate = FrameRate;
        v.ResolutionWidth = ResolutionWidth;
        v.ResolutionHeight = ResolutionHeight;
        v.Codec = Codec;
        v.BitrateMode = BitrateMode;
        v.BitrateKbps = BitrateKbps;
        v.GopSize = GopSize;
        v.RenderEngine = RenderEngine;
        v.OutputName = OutputFileName;
        v.Duration = Duration;
        if (!string.IsNullOrEmpty(SelectedFormat))
        {
             // Although we don't store Format in VideoSettings explicitly in previous code, 
             // if we wanted to persist it we should add it to AppConfig.VideoSettings model.
             // For now, assuming format selection is transient or handled elsewhere if not in model.
        }

        _configService.SaveConfiguration();
        StatusMessage = "Settings saved.";
        await Task.CompletedTask;
    }
}
