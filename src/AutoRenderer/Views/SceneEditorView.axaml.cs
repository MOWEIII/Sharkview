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

    private async void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            var viewModel = DataContext as SceneEditorViewModel;

            if (viewModel != null && files != null)
            {
                foreach (var file in files)
                {
                    var path = file.Path.LocalPath;
                    await viewModel.ImportModelFromPath(path);
                }
            }
        }
    }
}
