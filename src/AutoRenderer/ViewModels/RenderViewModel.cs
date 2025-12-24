using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using AutoRenderer.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoRenderer.ViewModels;

public partial class RenderViewModel : ViewModelBase
{
    private readonly IRenderService _renderService;
    private readonly IConfigurationService _configService;
    private readonly SceneEditorViewModel _sceneEditorViewModel;

    [ObservableProperty]
    private string _outputFileName = "output.mp4";

    [ObservableProperty]
    private double _duration = 5.0;

    [ObservableProperty]
    private string _selectedFormat = "MP4";

    public ObservableCollection<string> AvailableFormats { get; } = new() { "MP4", "AVI", "MKV" };

    [ObservableProperty]
    private string _renderStatus = "Idle";

    [ObservableProperty]
    private bool _isRendering;

    public RenderViewModel(
        IRenderService renderService, 
        IConfigurationService configService,
        SceneEditorViewModel sceneEditorViewModel)
    {
        _renderService = renderService;
        _configService = configService;
        _sceneEditorViewModel = sceneEditorViewModel;
    }

    [RelayCommand]
    private async Task Render()
    {
        if (IsRendering) return;

        try
        {
            IsRendering = true;
            RenderStatus = "Rendering... (This may take a while)";
            
            var outputDir = _configService.Config.DefaultOutputPath;
            if (string.IsNullOrEmpty(outputDir))
            {
                RenderStatus = "Error: Output directory not set.";
                return;
            }

            // Ensure filename has correct extension
            var fileName = OutputFileName;
            var ext = SelectedFormat == "MP4" ? ".mp4" : (SelectedFormat == "AVI" ? ".avi" : ".mkv");
            if (!fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                fileName = System.IO.Path.ChangeExtension(fileName, ext);
            }

            await _renderService.RenderSceneAsync(
                _sceneEditorViewModel.SceneObjects, 
                _sceneEditorViewModel.WorldSettings, // Added WorldSettings
                outputDir, 
                fileName,
                Duration,
                SelectedFormat);

            RenderStatus = $"Rendering Completed Successfully! Saved to {fileName}";
        }
        catch (Exception ex)
        {
            RenderStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsRendering = false;
        }
    }
}
