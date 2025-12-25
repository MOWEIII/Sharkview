using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AutoRenderer.ViewModels;
using System.Linq;
using AutoRenderer.Core.Models;

namespace AutoRenderer.Views;

public partial class SceneEditorView : UserControl
{
    private IBrush? _originalBackground;

    public SceneEditorView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Handled) return;

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
                _ = vm.ImportOrUnzip(path);
            }
        }
    }

    public void OnItemDragEnter(object? sender, DragEventArgs e)
    {
        if (sender is ContentControl control && control.DataContext is SceneObject targetObject && targetObject is not LightObject)
        {
#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
            var files = e.Data.GetFiles();
#pragma warning restore CS0618
            var file = files?.FirstOrDefault();
            if (file != null)
            {
                 var path = file.Path.LocalPath;
                 var ext = System.IO.Path.GetExtension(path).ToLower();
                 if (new[] { ".fbx", ".obj", ".glb", ".stl", ".zip" }.Contains(ext))
                 {
                     _originalBackground = control.Background;
                     control.Background = new SolidColorBrush(Color.Parse("#333399FF")); // Highlight color
                     ToolTip.SetTip(control, $"Replace with {System.IO.Path.GetFileName(path)}");
                     ToolTip.SetIsOpen(control, true);
                     e.DragEffects = DragDropEffects.Copy;
                 }
            }
        }
    }

    public void OnItemDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is ContentControl control)
        {
            if (_originalBackground != null)
            {
                control.Background = _originalBackground;
                _originalBackground = null;
            }
            ToolTip.SetIsOpen(control, false);
            ToolTip.SetTip(control, null);
        }
    }

    public void OnItemDrop(object? sender, DragEventArgs e)
    {
        // Clean up UI state first
        if (sender is ContentControl control)
        {
             if (_originalBackground != null)
            {
                control.Background = _originalBackground;
                _originalBackground = null;
            }
            ToolTip.SetIsOpen(control, false);
            ToolTip.SetTip(control, null);
        }
        
#pragma warning disable CS0618 // DragEventArgs.Data is obsolete
        var files = e.Data.GetFiles();
#pragma warning restore CS0618
        if (files != null)
        {
            var file = files.FirstOrDefault();
            if (file != null && sender is ContentControl ctrl && ctrl.DataContext is SceneObject targetObject && DataContext is SceneEditorViewModel vm)
            {
                // Only replace if it's not a LightObject
                if (targetObject is LightObject) return;

                var path = file.Path.LocalPath;
                var ext = System.IO.Path.GetExtension(path).ToLower();
                if (new[] { ".fbx", ".obj", ".glb", ".stl", ".zip" }.Contains(ext))
                {
                    if (ext == ".zip")
                    {
                        _ = vm.ReplaceModelWithZip(targetObject, path);
                    }
                    else
                    {
                        _ = vm.ReplaceModel(targetObject, path);
                    }
                    e.Handled = true;
                }
            }
        }
    }

    private void OnListBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && DataContext is SceneEditorViewModel vm)
        {
            vm.RemoveObjectCommand.Execute(null);
        }
    }
}
