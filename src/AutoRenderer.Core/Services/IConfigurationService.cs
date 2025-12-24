using AutoRenderer.Core.Models;

namespace AutoRenderer.Core.Services;

public interface IConfigurationService
{
    AppConfig Config { get; }
    void LoadConfiguration();
    void SaveConfiguration();
    void UpdateBlenderPath(string path);
    void UpdateDefaultOutputPath(string path);
}
