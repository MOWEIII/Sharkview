using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text;

namespace AutoRenderer.Core.Services;

public partial class ConsoleService : ObservableObject, IConsoleService
{
    [ObservableProperty]
    private string _logs = "Console initialized...\n";

    private readonly StringBuilder _logBuilder = new StringBuilder();

    public ConsoleService()
    {
        _logBuilder.Append(Logs);
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var formatted = $"[{timestamp}] {message}\n";
        
        _logBuilder.Append(formatted);
        
        // Update property (triggers UI)
        Logs = _logBuilder.ToString();
    }

    public void Clear()
    {
        _logBuilder.Clear();
        Logs = "";
    }
}
