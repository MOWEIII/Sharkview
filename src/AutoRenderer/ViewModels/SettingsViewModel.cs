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
        // Manual validation if needed, but Browse updates config immediately in this logic.
        // We can just save just in case.
        _configService.SaveConfiguration();
        StatusMessage = "Settings saved.";
        await Task.CompletedTask;
    }
}
