using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AutoRenderer.ViewModels;
using AutoRenderer.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using AutoRenderer.Core.Services;
using AutoRenderer.Services;

namespace AutoRenderer;

public partial class App : Application
{
    public new static App? Current => Application.Current as App;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Register services
        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();

        // Initialize configuration
        var configService = Services.GetRequiredService<IConfigurationService>();
        configService.LoadConfiguration();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            DisableAvaloniaDataAnnotationValidation();
            
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
            
            // Configure FilePickerService with the main window's storage provider
            var filePickerService = Services.GetRequiredService<IFilePickerService>() as FilePickerService;
            if (filePickerService != null)
            {
                filePickerService.SetStorageProvider(desktop.MainWindow.StorageProvider);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IBlenderService, BlenderService>();
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<IRenderService, RenderService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<SceneEditorViewModel>();
        services.AddSingleton<RenderViewModel>();
        services.AddSingleton<ConsoleViewModel>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
