using Avalonia.Controls;
using Avalonia.Input;
using AutoRenderer.ViewModels;
using System.Linq;

namespace AutoRenderer.Views;

public partial class SceneEditorView : UserControl
{
    public SceneEditorView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        // Get files from DragEventArgs
#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
        var files = e.Data.GetFiles();
#pragma warning restore CS0618
        if (files != null)
        {
            var file = files.FirstOrDefault();
            if (file != null && DataContext is SceneEditorViewModel vm)
            {
                // File path is usually a Uri, convert to local path
                var path = file.Path.LocalPath;
                // Import logic is async but we are in a void event handler.
                // Fire and forget is acceptable here for drag & drop UI interaction.
                _ = vm.ImportModelFromPath(path);
            }
        }
    }
}
