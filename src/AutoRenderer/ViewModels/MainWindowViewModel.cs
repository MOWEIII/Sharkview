using AutoRenderer.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoRenderer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IBlenderService _blenderService;
    
    [ObservableProperty]
    private ViewModelBase _currentView;
    
    [ObservableProperty]
    private string _statusMessage = "Ready";

    public SettingsViewModel SettingsViewModel { get; }
    public SceneEditorViewModel SceneEditorViewModel { get; }
    public RenderViewModel RenderViewModel { get; }
    public ConsoleViewModel ConsoleViewModel { get; }

    public MainWindowViewModel(
        IConfigurationService configService, 
        IBlenderService blenderService,
        SettingsViewModel settingsViewModel,
        SceneEditorViewModel sceneEditorViewModel,
        RenderViewModel renderViewModel,
        ConsoleViewModel consoleViewModel)
    {
        _configService = configService;
        _blenderService = blenderService;
        SettingsViewModel = settingsViewModel;
        SceneEditorViewModel = sceneEditorViewModel;
        RenderViewModel = renderViewModel;
        ConsoleViewModel = consoleViewModel;
        
        // Default view
        CurrentView = SceneEditorViewModel;

        // Initial check
        if (string.IsNullOrEmpty(_configService.Config.BlenderPath))
        {
            StatusMessage = "Blender path not configured. Please go to Settings.";
            CurrentView = SettingsViewModel;
        }
    }

    [RelayCommand]
    public void NavigateToSettings()
    {
        CurrentView = SettingsViewModel;
    }

    [RelayCommand]
    public void NavigateToEditor()
    {
        CurrentView = SceneEditorViewModel;
    }

    [RelayCommand]
    public void NavigateToRender()
    {
        CurrentView = RenderViewModel;
    }

    [RelayCommand]
    public void NavigateToConsole()
    {
        CurrentView = ConsoleViewModel;
    }
}
