using System.ComponentModel;

namespace AutoRenderer.Core.Services;

public interface IConsoleService : INotifyPropertyChanged
{
    string Logs { get; }
    string LatestLog { get; }
    void Log(string message);
    void Clear();
}
