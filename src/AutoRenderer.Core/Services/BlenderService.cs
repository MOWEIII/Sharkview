using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoRenderer.Core.Services;

public class BlenderService : IBlenderService
{
    public async Task<string?> AutoDetectBlenderPathAsync()
    {
        // Common paths to check based on OS
        string[] searchPaths = Array.Empty<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            searchPaths = new[]
            {
                @"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.0\blender.exe", // Fallback check
                @"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            searchPaths = new[]
            {
                "/usr/bin/blender",
                "/snap/bin/blender"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            searchPaths = new[]
            {
                "/Applications/Blender.app/Contents/MacOS/Blender"
            };
        }

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                if (await ValidateBlenderPathAsync(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    public async Task<string> GetBlenderVersionAsync(string path)
    {
        if (!File.Exists(path)) return string.Empty;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return string.Empty;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse version from output (e.g., "Blender 5.0.0")
            // Output usually starts with "Blender X.X.X"
            var lines = output.Split('\n');
            if (lines.Length > 0)
            {
                var firstLine = lines[0];
                var match = Regex.Match(firstLine, @"Blender\s+(\d+\.\d+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<bool> ValidateBlenderPathAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        var version = await GetBlenderVersionAsync(path);
        
        // Simple validation: must return a version string starting with a number
        // We can enforce 5.0 requirement here if needed
        if (string.IsNullOrEmpty(version)) return false;
        
        // Optional: Check for 5.0+
        // float.TryParse(version, out float v) && v >= 5.0
        
        return true;
    }

    public async Task<string[]> GetStudioLightsAsync(string blenderPath)
    {
        await Task.Yield(); // Ensure async signature is valid even if we do sync IO
        
        if (string.IsNullOrEmpty(blenderPath) || !File.Exists(blenderPath))
            return Array.Empty<string>();

        var installDir = Path.GetDirectoryName(blenderPath);
        if (string.IsNullOrEmpty(installDir)) return Array.Empty<string>();

        try
        {
            // Find version directory (e.g. "4.0", "3.6")
            var versionDirs = Directory.GetDirectories(installDir);
            
            foreach (var dir in versionDirs)
            {
                var dirName = Path.GetFileName(dir);
                // Check if it looks like a version number (X.X)
                if (Regex.IsMatch(dirName, @"^\d+\.\d+$"))
                {
                    var studioLightPath = Path.Combine(dir, "datafiles", "studiolights", "world");
                    if (Directory.Exists(studioLightPath))
                    {
                        var files = new List<string>();
                        files.AddRange(Directory.GetFiles(studioLightPath, "*.exr"));
                        files.AddRange(Directory.GetFiles(studioLightPath, "*.hdr"));
                        return files.ToArray();
                    }
                }
            }
        }
        catch
        {
            // Ignore permission errors etc
        }
        
        return Array.Empty<string>();
    }
}
