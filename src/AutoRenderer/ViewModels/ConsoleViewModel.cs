using AutoRenderer.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoRenderer.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
    private readonly IConsoleService _consoleService;

    public string Logs => _consoleService.Logs;

    public ConsoleViewModel(IConsoleService consoleService)
    {
        _consoleService = consoleService;
        _consoleService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IConsoleService.Logs))
            {
                OnPropertyChanged(nameof(Logs));
            }
        };
    }

    [RelayCommand]
    public void ClearLogs()
    {
        _consoleService.Clear();
    }
}
