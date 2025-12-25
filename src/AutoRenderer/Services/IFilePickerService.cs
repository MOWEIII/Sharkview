using System.Threading.Tasks;

namespace AutoRenderer.Services;

public interface IFilePickerService
{
    Task<string?> PickFileAsync(string title, string[] extensions);
    Task<string?> PickSaveFileAsync(string title, string defaultExtension);
    Task<string?> PickFolderAsync(string title);
}
