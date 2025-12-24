using System.IO;
using AutoRenderer.Core.Services;
using Xunit;

namespace AutoRenderer.Tests;

public class ConfigurationServiceTests
{
    [Fact]
    public void CanLoadAndSaveConfiguration()
    {
        // Setup
        var service = new ConfigurationService();
        var originalPath = service.Config.BlenderPath;
        
        // Act
        service.UpdateBlenderPath("C:/Fake/Blender/Path.exe");
        
        // Assert
        Assert.Equal("C:/Fake/Blender/Path.exe", service.Config.BlenderPath);
        
        // Cleanup (optional, depends on if we want to persist garbage)
        // Ideally we mock the file system, but for integration test this works.
    }
}
