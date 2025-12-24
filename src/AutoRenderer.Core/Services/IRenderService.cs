using System.Collections.Generic;
using System.Threading.Tasks;
using AutoRenderer.Core.Models;

namespace AutoRenderer.Core.Services;

public interface IRenderService
{
    Task RenderSceneAsync(IEnumerable<SceneObject> objects, WorldSettings world, string outputPath, string fileName, double duration = 5.0, string format = "MP4");
    Task<string> RenderPreviewAsync(IEnumerable<SceneObject> objects, WorldSettings world, string outputPath);
    Task InitializeAsync();
    void Shutdown();
}
