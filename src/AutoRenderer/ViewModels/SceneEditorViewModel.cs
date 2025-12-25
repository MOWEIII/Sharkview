using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AutoRenderer.Core.Models;
using AutoRenderer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media.Imaging;
using System.IO;
using System.IO.Compression;
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
    private readonly IConfigurationService _configService;
    private readonly IBlenderService _blenderService;
    private DispatcherTimer _debounceTimer;

    [ObservableProperty]
    private ObservableCollection<SceneObject> _sceneObjects = new();

    [ObservableProperty]
    private ObservableCollection<PresetItem> _availablePresets = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModelSelected))]
    private SceneObject? _selectedObject;

    public bool IsModelSelected => SelectedObject != null && SelectedObject is not LightObject;

    [ObservableProperty]
    private WorldSettings _worldSettings = new();

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private bool _isLoadingPreview;

    public SceneEditorViewModel(
        IFilePickerService filePickerService,
        IRenderService renderService,
        IConsoleService consoleService,
        IConfigurationService configService,
        IBlenderService blenderService)
    {
        _filePickerService = filePickerService;
        _renderService = renderService;
        _consoleService = consoleService;
        _configService = configService;
        _blenderService = blenderService;

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
        
        LoadPresets();
    }

    private async void LoadPresets()
    {
        try 
        {
            var blenderPath = _configService.Config.BlenderPath;
            if (!string.IsNullOrEmpty(blenderPath))
             {
                 var presets = await _blenderService.GetStudioLightsAsync(blenderPath);
                AvailablePresets = new ObservableCollection<PresetItem>(
                    presets.Select(p => new PresetItem { Name = Path.GetFileNameWithoutExtension(p), Path = p })
                );

                // Default to forest if available
                var forest = AvailablePresets.FirstOrDefault(p => p.Name.Equals("forest", StringComparison.OrdinalIgnoreCase));
                if (forest != null)
                {
                    WorldSettings.EnvironmentTexturePath = forest.Path;
                }
            }
         }
        catch (Exception ex)
        {
            _consoleService.Log($"Failed to load studio light presets: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BrowseEnvironmentTexture()
    {
        var path = await _filePickerService.PickFileAsync("Select Environment Texture", new[] { "*.exr", "*.hdr", "*.jpg", "*.png" });
        if (!string.IsNullOrEmpty(path))
        {
            WorldSettings.EnvironmentTexturePath = path;
            WorldSettings.EnvironmentType = EnvironmentType.CustomImage;
        }
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
        var videoSettings = configService?.Config.VideoSettings;
        var outputName = videoSettings?.OutputName;
        var duration = videoSettings?.Duration ?? 5.0;

        if (string.IsNullOrEmpty(outputPath)) outputPath = Path.GetTempPath();
        if (string.IsNullOrEmpty(outputName)) outputName = "Render";

        var fileName = $"{outputName}_{DateTime.Now:yyyyMMdd_HHmmss}";
        
        _consoleService.Log("Starting Quick Render...");
        try 
        {
            if (outputPath != null)
                await _renderService.RenderSceneAsync(SceneObjects, WorldSettings, outputPath, fileName, duration);
        }
        catch (Exception ex)
        {
            _consoleService.Log($"Quick Render Failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ReplaceSelectedModel()
    {
        if (SelectedObject == null || !IsModelSelected) return;

        var path = await _filePickerService.PickFileAsync("Replace 3D Model", new[] { "*.fbx", "*.obj", "*.glb", "*.stl" });
        if (!string.IsNullOrEmpty(path))
        {
            await ReplaceModel(SelectedObject, path);
        }
    }

    public async Task ReplaceModel(SceneObject target, string newPath)
    {
        // Basic validation
        var ext = Path.GetExtension(newPath).ToLower();
        if (new[] { ".fbx", ".obj", ".glb", ".stl" }.Contains(ext))
        {
             target.FilePath = newPath;
             target.Name = Path.GetFileNameWithoutExtension(newPath);
             
             // Trigger preview refresh
             await RefreshPreview();
        }
    }

    public async Task ReplaceModelWithZip(SceneObject target, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var extractPath = Path.Combine(dir ?? Path.GetTempPath(), name + "_extracted");
            
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);
                
            Directory.CreateDirectory(extractPath);

            ZipFile.ExtractToDirectory(path, extractPath);
            
            var modelFiles = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                .Where(f => new[] { ".fbx", ".obj", ".glb", ".stl" }.Contains(Path.GetExtension(f).ToLower()))
                .ToList();
                
            if (modelFiles.Any())
            {
                // Use the first model found to replace
                await ReplaceModel(target, modelFiles.First());
            }
            else
            {
                _consoleService.Log("No supported model files found in ZIP archive.");
            }

            // Cleanup extracted files after use
            try 
            {
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
            }
            catch (Exception cleanupEx)
            {
                _consoleService.Log($"Warning: Could not cleanup temp files: {cleanupEx.Message}");
            }
        }
        catch (Exception ex)
        {
            _consoleService.Log($"Error replacing model from ZIP: {ex.Message}");
        }
    }

    public async Task ImportOrUnzip(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        if (ext == ".zip")
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var extractPath = Path.Combine(dir ?? Path.GetTempPath(), name + "_extracted");
                
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                    
                Directory.CreateDirectory(extractPath);

                ZipFile.ExtractToDirectory(path, extractPath);
                
                var modelFiles = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => new[] { ".fbx", ".obj", ".glb", ".stl" }.Contains(Path.GetExtension(f).ToLower()));
                    
                foreach (var modelFile in modelFiles)
                {
                    await ImportModelFromPath(modelFile);
                }

                // Cleanup extracted files after use
                try 
                {
                    if (Directory.Exists(extractPath))
                        Directory.Delete(extractPath, true);
                }
                catch (Exception cleanupEx)
                {
                    _consoleService.Log($"Warning: Could not cleanup temp files: {cleanupEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _consoleService.Log($"Error extracting ZIP: {ex.Message}");
            }
        }
        else
        {
            await ImportModelFromPath(path);
        }
    }
}

public class PresetItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}
