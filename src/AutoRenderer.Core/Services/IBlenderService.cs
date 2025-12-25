using System.Threading.Tasks;

namespace AutoRenderer.Core.Services;

public interface IBlenderService
{
    Task<bool> ValidateBlenderPathAsync(string path);
    Task<string?> AutoDetectBlenderPathAsync();
    Task<string> GetBlenderVersionAsync(string path);
    Task<string[]> GetStudioLightsAsync(string blenderPath);
}
