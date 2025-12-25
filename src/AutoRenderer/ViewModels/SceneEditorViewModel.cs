using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AutoRenderer.Core.Models;
using AutoRenderer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media.Imaging;
using System.IO;
using AutoRenderer.Core.Services;
using System.Linq;
using System.ComponentModel;
using System;
using Avalonia.Threading;

namespace AutoRenderer.ViewModels;

public partial class SceneEditorViewModel : ViewModelBase
{
    private readonly IFilePickerService _filePickerService;
    private readonly IRenderService _renderService;
    private readonly IConsoleService _consoleService;
    private DispatcherTimer _debounceTimer;

    [ObservableProperty]
    private ObservableCollection<SceneObject> _sceneObjects = new();

    [ObservableProperty]
    private SceneObject? _selectedObject;

    [ObservableProperty]
    private WorldSettings _worldSettings = new();

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private bool _isLoadingPreview;

    public SceneEditorViewModel(
        IFilePickerService filePickerService,
        IRenderService renderService,
        IConsoleService consoleService)
    {
        _filePickerService = filePickerService;
        _renderService = renderService;
        _consoleService = consoleService;

        // Initialize Debounce Timer
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _debounceTimer.Tick += (s, e) => 
        {
            _debounceTimer.Stop();
            RefreshPreviewCommand.Execute(null);
        };

        // Watch for world settings changes
        _worldSettings.PropertyChanged += OnPropertyChanged;
        
        // Initial state
        IsManualCamera = !WorldSettings.AutoCamera;

        // Initialize Render Service
        _ = _renderService.InitializeAsync();
    }

    [ObservableProperty]
    private bool _isManualCamera;

    [ObservableProperty]
    private bool _autoRefreshPreview = true;

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == WorldSettings && e.PropertyName == nameof(WorldSettings.AutoCamera))
        {
            IsManualCamera = !WorldSettings.AutoCamera;
        }

        // Trigger debounced refresh if enabled
        if (AutoRefreshPreview)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    [RelayCommand]
    private async Task ImportModel()
    {
        var path = await _filePickerService.PickFileAsync("Import 3D Model", new[] { "*.fbx", "*.obj", "*.glb", "*.stl" });
        if (!string.IsNullOrEmpty(path))
        {
            await ImportModelFromPath(path);
        }
    }

    public async Task ImportModelFromPath(string path)
    {
        // Basic validation
        var ext = Path.GetExtension(path).ToLower();
        if (new[] { ".fbx", ".obj", ".glb", ".stl" }.Contains(ext))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var obj = new SceneObject
            {
                Name = name,
                FilePath = path,
                Position = new ObservableVector3(0, 0, 0),
                Rotation = new ObservableVector3(0, 0, 0),
                Scale = new ObservableVector3(1, 1, 1)
            };
            AttachPropertyListeners(obj);
            SceneObjects.Add(obj);
            SelectedObject = obj;
            
            // Trigger preview refresh immediately
            await RefreshPreview();
        }
    }

    [RelayCommand]
    private async Task AddLight()
    {
        var light = new LightObject
        {
            Name = "Point Light",
            Position = new ObservableVector3(5, -5, 10), // Default position similar to previous hardcoded light
            LightType = LightType.POINT,
            Energy = 1000.0f
        };
        AttachPropertyListeners(light);
        SceneObjects.Add(light);
        SelectedObject = light;
        await RefreshPreview();
    }

    [RelayCommand]
    private void EditWorldSettings()
    {
        SelectedObject = null;
    }

    private void AttachPropertyListeners(SceneObject obj)
    {
        obj.PropertyChanged += OnPropertyChanged;
        obj.Position.PropertyChanged += OnPropertyChanged;
        obj.Rotation.PropertyChanged += OnPropertyChanged;
        obj.Scale.PropertyChanged += OnPropertyChanged;
        
        if (obj is LightObject light)
        {
            // Light specific properties are covered by obj.PropertyChanged if ObservableProperty is used
        }
    }

    [RelayCommand]
    private void AddAutoRotateModifier()
    {
        if (SelectedObject != null)
        {
            var mod = new AutoRotateModifier();
            mod.PropertyChanged += OnPropertyChanged;
            SelectedObject.Modifiers.Add(mod);
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedObject.Modifiers)));
        }
    }

    [RelayCommand]
    private void RemoveModifier(Modifier modifier)
    {
        if (SelectedObject != null && modifier != null)
        {
            modifier.PropertyChanged -= OnPropertyChanged;
            SelectedObject.Modifiers.Remove(modifier);
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedObject.Modifiers)));
        }
    }

    [RelayCommand]
    private void RemoveObject()
    {
        if (SelectedObject != null)
        {
            // Detach listeners
            SelectedObject.PropertyChanged -= OnPropertyChanged;
            SelectedObject.Position.PropertyChanged -= OnPropertyChanged;
            SelectedObject.Rotation.PropertyChanged -= OnPropertyChanged;
            SelectedObject.Scale.PropertyChanged -= OnPropertyChanged;

            SceneObjects.Remove(SelectedObject);
            SelectedObject = null;
            
            // Refresh to show empty scene or updated scene
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    [ObservableProperty]
    private string _previewStatusMessage = "";

    [RelayCommand]
    private async Task RefreshPreview()
    {
        if (IsLoadingPreview) return;
        
        try
        {
            IsLoadingPreview = true;
            PreviewStatusMessage = "";
            _consoleService.Log($"Starting Preview Render: {DateTime.Now}");
            
            // Create a temp path for the preview image
            var tempPath = Path.Combine(Path.GetTempPath(), "autorender_preview.png");
            
            var log = await _renderService.RenderPreviewAsync(SceneObjects, WorldSettings, tempPath);
            if (!string.IsNullOrEmpty(log) && log != "OK") _consoleService.Log(log);

            if (File.Exists(tempPath))
            {
                using var stream = File.OpenRead(tempPath);
                PreviewImage = new Bitmap(stream);
                _consoleService.Log("Preview Image Loaded.");
            }
            else
            {
                 _consoleService.Log("Preview image file not found.");
            }
        }
        catch (System.Exception ex)
        {
            PreviewStatusMessage = $"Render Error: {ex.Message}";
            _consoleService.Log($"[EXCEPTION] {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    [RelayCommand]
    private async Task SavePreviewAsImage()
    {
        if (PreviewImage == null) return;

        var path = await _filePickerService.PickSaveFileAsync("Save Preview Image", "png");
        if (!string.IsNullOrEmpty(path))
        {
             try
             {
                 PreviewImage.Save(path);
                 _consoleService.Log($"Preview saved to: {path}");
             }
             catch (Exception ex)
             {
                 _consoleService.Log($"Error saving image: {ex.Message}");
             }
        }
    }

    [RelayCommand]
    private async Task QuickRender()
    {
        // For quick render, we'll use the default output path and a timestamped filename
        var configService = App.Current?.Services?.GetService(typeof(IConfigurationService)) as IConfigurationService;
        var outputPath = configService?.Config.DefaultOutputPath;

        if (string.IsNullOrEmpty(outputPath)) outputPath = Path.GetTempPath();

        var fileName = $"Render_{DateTime.Now:yyyyMMdd_HHmmss}";
        
        _consoleService.Log("Starting Quick Render...");
        try 
        {
            if (outputPath != null)
                await _renderService.RenderSceneAsync(SceneObjects, WorldSettings, outputPath, fileName);
        }
        catch (Exception ex)
        {
            _consoleService.Log($"Quick Render Failed: {ex.Message}");
        }
    }
}
