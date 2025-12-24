using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace AutoRenderer.Services;

public class FilePickerService : IFilePickerService
{
    private IStorageProvider? _storageProvider;

    public void SetStorageProvider(IStorageProvider provider)
    {
        _storageProvider = provider;
    }

    public async Task<string?> PickFileAsync(string title, string[] extensions)
    {
        if (_storageProvider == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Executable")
                {
                    Patterns = extensions
                }
            }
        };

        var files = await _storageProvider.OpenFilePickerAsync(options);
        return files.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        if (_storageProvider == null) return null;

        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        var folders = await _storageProvider.OpenFolderPickerAsync(options);
        return folders.FirstOrDefault()?.Path.LocalPath;
    }
}
