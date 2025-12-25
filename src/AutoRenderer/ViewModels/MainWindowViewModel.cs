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
    public ConsoleViewModel ConsoleViewModel { get; }

    private readonly IConsoleService _consoleService;
    [ObservableProperty] private string _latestLog = "";

    public MainWindowViewModel(
        IConfigurationService configService, 
        IBlenderService blenderService,
        IConsoleService consoleService,
        SettingsViewModel settingsViewModel,
        SceneEditorViewModel sceneEditorViewModel,
        ConsoleViewModel consoleViewModel)
    {
        _configService = configService;
        _blenderService = blenderService;
        _consoleService = consoleService;
        SettingsViewModel = settingsViewModel;
        SceneEditorViewModel = sceneEditorViewModel;
        ConsoleViewModel = consoleViewModel;
        
        // Bind to ConsoleService
        _consoleService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IConsoleService.LatestLog))
            {
                LatestLog = _consoleService.LatestLog;
            }
        };
        LatestLog = _consoleService.LatestLog; // Initial value

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
    public void NavigateToConsole()
    {
        CurrentView = ConsoleViewModel;
    }
}
