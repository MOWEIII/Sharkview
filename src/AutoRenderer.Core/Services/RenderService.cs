using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AutoRenderer.Core.Models;
using AutoRenderer.Core.Utilities;

namespace AutoRenderer.Core.Services;

public class RenderService : IRenderService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly IConsoleService _consoleService;

    public RenderService(IConfigurationService configService, IConsoleService consoleService)
    {
        _configService = configService;
        _consoleService = consoleService;
    }

    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }

    public async Task RenderSceneAsync(IEnumerable<SceneObject> objects, WorldSettings world, string outputPath, string fileName, double duration = 5.0, string format = "MP4")
    {
        // Ensure output directory exists
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            _consoleService.Log($"Created output directory: {outputPath}");
        }

        var blenderPath = _configService.Config.BlenderPath;
        if (string.IsNullOrEmpty(blenderPath) || !File.Exists(blenderPath))
        {
            _consoleService.Log("Error: Blender executable not found.");
            throw new FileNotFoundException("Blender executable not found. Please configure the path in settings.");
        }

        var fullOutputPath = Path.Combine(outputPath, fileName);
        // Ensure extension
        var ext = format == "MP4" ? ".mp4" : (format == "AVI" ? ".avi" : ".mkv");
        if (!fullOutputPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
        {
            fullOutputPath += ext;
        }

        _consoleService.Log($"Starting Render: {fileName}");
        _consoleService.Log($"Output Path: {fullOutputPath}");

        var scriptPath = Path.Combine(Path.GetTempPath(), $"autorender_script_{Guid.NewGuid()}.py");
        
        // Use configured FPS
        var videoSettings = _configService.Config.VideoSettings;
        int fps = videoSettings.FrameRate;
        int frameEnd = (int)(duration * fps);
        
        var pythonScript = GeneratePythonScript(objects, world, fullOutputPath, frameEnd, format, false, videoSettings);
        
        await File.WriteAllTextAsync(scriptPath, pythonScript);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = $"--background --python \"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _consoleService.Log("Launching Blender process...");
            using var process = new Process { StartInfo = startInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _consoleService.Log($"[Blender] {e.Data}");
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _consoleService.Log($"[Blender Error] {e.Data}");
                }
            };

            process.Start();
            ChildProcessTracker.AddProcess(process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _consoleService.Log($"Render Failed with Exit Code: {process.ExitCode}");
                throw new Exception($"Blender rendering failed with exit code {process.ExitCode}. Check console logs for details.");
            }
            else
            {
                _consoleService.Log("Render Complete!");
                // Check if file exists (Blender might have appended frame range)
                if (File.Exists(fullOutputPath))
                {
                    _consoleService.Log($"File saved at: {fullOutputPath}");
                }
                else
                {
                    // Check for frame range file (e.g. video0001-0150.mp4)
                    var dir = Path.GetDirectoryName(fullOutputPath);
                    if (dir != null)
                    {
                        var name = Path.GetFileNameWithoutExtension(fullOutputPath);
                        var files = Directory.GetFiles(dir, $"{name}*{ext}");
                        if (files.Length > 0)
                        {
                             _consoleService.Log($"File saved at (Blender appended frames): {files[0]}");
                        }
                        else
                        {
                            _consoleService.Log($"Warning: Output file not found exactly at {fullOutputPath}. Check folder content.");
                        }
                    }
                    else
                    {
                         _consoleService.Log($"Warning: Output directory invalid: {fullOutputPath}.");
                    }
                }
            }
        }
        finally
        {
            try
            {
                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                    // _consoleService.Log($"Cleaned up temp script: {scriptPath}");
                }
            }
            catch (Exception ex)
            {
                _consoleService.Log($"Warning: Failed to delete temp script: {ex.Message}");
            }
        }
    }

    public async Task<string> RenderPreviewAsync(IEnumerable<SceneObject> objects, WorldSettings world, string outputPath)
    {
        if (_blenderProcess == null || _blenderProcess.HasExited)
        {
            await InitializeAsync();
        }

        var script = GeneratePythonScript(objects, world, outputPath, 1, "PNG", true);
        
        // Retry logic with timeout
        int maxRetries = 2;
        for (int retry = 0; retry <= maxRetries; retry++)
        {
            try 
            {
                // Set a timeout for the entire operation
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                
                using var client = new System.Net.Sockets.TcpClient();
                // Connect with timeout
                await client.ConnectAsync("127.0.0.1", ServerPort, cts.Token);
                
                using var stream = client.GetStream();
                
                // Send
                var data = Encoding.UTF8.GetBytes(script);
                var header = BitConverter.GetBytes((uint)data.Length); // Little Endian
                if (!BitConverter.IsLittleEndian) Array.Reverse(header);
                
                await stream.WriteAsync(header, cts.Token);
                await stream.WriteAsync(data, cts.Token);
                
                // Receive Response Header
                var respHeader = new byte[4];
                await stream.ReadExactlyAsync(respHeader, cts.Token);
                var respLen = BitConverter.ToUInt32(respHeader);
                if (!BitConverter.IsLittleEndian) 
                {
                    // Note: Manually swap if needed
                }
                
                var respBuffer = new byte[respLen];
                await stream.ReadExactlyAsync(respBuffer, cts.Token);
                var response = Encoding.UTF8.GetString(respBuffer);
                
                return response;
            }
            catch (OperationCanceledException)
            {
                _consoleService.Log($"Render Preview Timeout (Attempt {retry + 1}/{maxRetries + 1}). Restarting Blender Service...");
                Shutdown();
                if (retry < maxRetries) await InitializeAsync();
            }
            catch (Exception ex)
            {
                _consoleService.Log($"Socket Error: {ex.Message} (Attempt {retry + 1}/{maxRetries + 1}). Restarting Blender Service...");
                Shutdown();
                if (retry < maxRetries) await InitializeAsync();
            }
        }

        return "Error: Render Preview Failed after retries.";
    }

    private string GeneratePythonScript(IEnumerable<SceneObject> objects, WorldSettings world, string outputFile, int frameEnd, string format, bool isPreview, VideoExportSettings? videoSettings = null)
    {
        if (videoSettings == null) videoSettings = new VideoExportSettings();

        var sb = new StringBuilder();
        
        // Imports
        sb.AppendLine("import bpy");
        sb.AppendLine("import math");
        sb.AppendLine("import mathutils"); // Added mathutils
        sb.AppendLine("import os");

        // Clear Scene
        // Use use_empty=False to ensure default addons (like FFMPEG IO) are loaded
        sb.AppendLine("bpy.ops.wm.read_factory_settings(use_empty=False)");
        sb.AppendLine("bpy.ops.object.select_all(action='SELECT')");
        sb.AppendLine("bpy.ops.object.delete()");

        // World Settings
        sb.AppendLine("# World Settings");
        sb.AppendLine("if bpy.context.scene.world is None:");
        sb.AppendLine("    bpy.context.scene.world = bpy.data.worlds.new('World')");
        
        sb.AppendLine("bpy.context.scene.world.use_nodes = True");
        sb.AppendLine("world = bpy.context.scene.world");
        sb.AppendLine("if world.node_tree:");
        sb.AppendLine("    world.node_tree.nodes.clear()");
        sb.AppendLine("    bg_node = world.node_tree.nodes.new(type='ShaderNodeBackground')");
        sb.AppendLine("    out_node = world.node_tree.nodes.new(type='ShaderNodeOutputWorld')");
        sb.AppendLine("    world.node_tree.links.new(bg_node.outputs[0], out_node.inputs[0])");
        
        if (world != null)
        {
            sb.AppendLine($"    bg_node.inputs[1].default_value = {world.Strength}"); // Strength
            
            if (world.EnvironmentType == EnvironmentType.SolidColor)
            {
                // Color
                if (!string.IsNullOrEmpty(world.BackgroundColor) && world.BackgroundColor.StartsWith("#") && world.BackgroundColor.Length >= 7)
                {
                    try {
                        var r = Convert.ToInt32(world.BackgroundColor.Substring(1, 2), 16) / 255.0f;
                        var g = Convert.ToInt32(world.BackgroundColor.Substring(3, 2), 16) / 255.0f;
                        var b = Convert.ToInt32(world.BackgroundColor.Substring(5, 2), 16) / 255.0f;
                        sb.AppendLine($"    bg_node.inputs[0].default_value = ({r}, {g}, {b}, 1)");
                    } catch {}
                }
            }
            else if ((world.EnvironmentType == EnvironmentType.StudioPreset || world.EnvironmentType == EnvironmentType.CustomImage) 
                     && !string.IsNullOrEmpty(world.EnvironmentTexturePath))
            {
                var texPath = world.EnvironmentTexturePath.Replace("\\", "/");
                sb.AppendLine($"    tex_node = world.node_tree.nodes.new(type='ShaderNodeTexEnvironment')");
                sb.AppendLine($"    try:");
                sb.AppendLine($"        tex_node.image = bpy.data.images.load('{texPath}')");
                
                if (world.ShowEnvironmentBackground)
                {
                    sb.AppendLine($"        world.node_tree.links.new(tex_node.outputs[0], bg_node.inputs[0])");
                }
                else
                {
                    // Mix Shader Setup for Transparent/Solid Background while keeping HDRI lighting
                    sb.AppendLine($"        # Mix Shader for invisible background");
                    sb.AppendLine($"        mix_node = world.node_tree.nodes.new(type='ShaderNodeMixShader')");
                    sb.AppendLine($"        path_node = world.node_tree.nodes.new(type='ShaderNodeLightPath')");
                    sb.AppendLine($"        solid_bg_node = world.node_tree.nodes.new(type='ShaderNodeBackground')");
                    
                    // Set Solid Color for Camera Rays
                    if (!string.IsNullOrEmpty(world.BackgroundColor) && world.BackgroundColor.StartsWith("#") && world.BackgroundColor.Length >= 7)
                    {
                        try {
                            var r = Convert.ToInt32(world.BackgroundColor.Substring(1, 2), 16) / 255.0f;
                            var g = Convert.ToInt32(world.BackgroundColor.Substring(3, 2), 16) / 255.0f;
                            var b = Convert.ToInt32(world.BackgroundColor.Substring(5, 2), 16) / 255.0f;
                            sb.AppendLine($"        solid_bg_node.inputs[0].default_value = ({r}, {g}, {b}, 1)");
                        } catch {}
                    }
                    sb.AppendLine($"        solid_bg_node.inputs[1].default_value = {world.Strength}");

                    // Connect Nodes
                    // Input 1 (Factor 0): HDRI Background (Lighting)
                    sb.AppendLine($"        world.node_tree.links.new(tex_node.outputs[0], bg_node.inputs[0])");
                    sb.AppendLine($"        world.node_tree.links.new(bg_node.outputs[0], mix_node.inputs[1])");
                    
                    // Input 2 (Factor 1): Solid Background (Camera Ray)
                    sb.AppendLine($"        world.node_tree.links.new(solid_bg_node.outputs[0], mix_node.inputs[2])");
                    
                    // Factor: Is Camera Ray
                    sb.AppendLine($"        world.node_tree.links.new(path_node.outputs['Is Camera Ray'], mix_node.inputs[0])");
                    
                    // Output
                    sb.AppendLine($"        world.node_tree.links.new(mix_node.outputs[0], out_node.inputs[0])");
                }

                sb.AppendLine($"    except Exception as e: print(f'Failed to load environment texture: {{e}}')");
            }
        }

        // Import Objects & Lights
        bool hasLights = false;
        foreach (var sceneObj in objects)
        {
            if (sceneObj is LightObject) hasLights = true;

            // Deselect all first
            sb.AppendLine("bpy.ops.object.select_all(action='DESELECT')");

            if (sceneObj is LightObject light)
            {
                sb.AppendLine($"# Light: {light.Name}");
                string typeStr = light.LightType.ToString(); // POINT, SUN, SPOT, AREA
                
                sb.AppendLine($"bpy.ops.object.light_add(type='{typeStr}', location=({light.Position.X}, {light.Position.Y}, {light.Position.Z}))");
                sb.AppendLine("light = bpy.context.object");
                sb.AppendLine($"light.data.energy = {light.Energy}");
                
                // Color Parsing
                if (!string.IsNullOrEmpty(light.Color) && light.Color.StartsWith("#") && light.Color.Length >= 7)
                {
                    try {
                        var r = Convert.ToInt32(light.Color.Substring(1, 2), 16) / 255.0f;
                        var g = Convert.ToInt32(light.Color.Substring(3, 2), 16) / 255.0f;
                        var b = Convert.ToInt32(light.Color.Substring(5, 2), 16) / 255.0f;
                        sb.AppendLine($"light.data.color = ({r}, {g}, {b})");
                    } catch {}
                }

                sb.AppendLine($"light.rotation_euler = (math.radians({light.Rotation.X}), math.radians({light.Rotation.Y}), math.radians({light.Rotation.Z}))");
            }
            else 
            {
                // Model Import
                var obj = sceneObj;
                var path = obj.FilePath.Replace("\\", "/");
                var ext = Path.GetExtension(path).ToLower();

                if (string.IsNullOrEmpty(path)) continue;

                sb.AppendLine($"# Import {obj.Name}");
                if (ext == ".fbx")
                    sb.AppendLine($"bpy.ops.import_scene.fbx(filepath='{path}')");
                else if (ext == ".obj")
                    sb.AppendLine($"bpy.ops.import_scene.obj(filepath='{path}')");
                else if (ext == ".glb" || ext == ".gltf")
                    sb.AppendLine($"bpy.ops.import_scene.gltf(filepath='{path}')");
                else if (ext == ".stl")
                    sb.AppendLine($"bpy.ops.import_mesh.stl(filepath='{path}')");
                
                // Apply Transform to imported objects (which are selected)
                sb.AppendLine($"for obj in bpy.context.selected_objects:");
                sb.AppendLine($"    # Ensure Euler Rotation Mode");
                sb.AppendLine($"    obj.rotation_mode = 'XYZ'");
                
                sb.AppendLine($"    # Remove any existing constraints to prevent conflicts");
                sb.AppendLine($"    for c in obj.constraints: obj.constraints.remove(c)");
                
                sb.AppendLine($"    # Only apply transform to root objects (those without a selected parent)");
                sb.AppendLine($"    # This prevents double-transformation of child objects");
                sb.AppendLine($"    if obj.parent is None or obj.parent not in bpy.context.selected_objects:");
                sb.AppendLine($"        obj.location = ({obj.Position.X}, {obj.Position.Y}, {obj.Position.Z})");
                sb.AppendLine($"        obj.rotation_euler = (math.radians({obj.Rotation.X}), math.radians({obj.Rotation.Y}), math.radians({obj.Rotation.Z}))");
                sb.AppendLine($"        obj.scale = ({obj.Scale.X}, {obj.Scale.Y}, {obj.Scale.Z})");
                sb.AppendLine($"        print(f'Applied Transform to {{obj.name}}: Pos={{obj.location}}, Rot={{obj.rotation_euler}}, Scale={{obj.scale}}')");

                // Apply Modifiers
                if (!isPreview)
                {
                    foreach (var mod in obj.Modifiers)
                    {
                        if (mod is AutoRotateModifier rotateMod)
                        {
                            sb.AppendLine("    # Auto Rotate Modifier");
                            sb.AppendLine("    obj.animation_data_create()");
                            sb.AppendLine("    obj.animation_data.action = bpy.data.actions.new(name='RotationAction')");
                            
                            sb.AppendLine($"    frames = {frameEnd}");
                            string axisIndex = rotateMod.Axis == "Y" ? "1" : (rotateMod.Axis == "X" ? "0" : "2");
                            
                            // Capture initial rotation for this axis to ensure relative rotation
                            sb.AppendLine($"    start_angle = obj.rotation_euler[{axisIndex}]");
                            
                            sb.AppendLine("    for f in range(frames + 1):");
                            
                            // Calculate angle based on Speed (Degrees/Second) and configured FPS
                            // f is current frame index
                            // Time t = f / fps
                            // Angle = t * Speed = (f / fps) * Speed
                            // The key is to ensure fpsVal matches the render FPS exactly
                            double fpsVal = videoSettings.FrameRate > 0 ? videoSettings.FrameRate : 30.0;
                            // Use consistent floating point division
                            sb.AppendLine($"        t = float(f) / float({fpsVal})");
                            sb.AppendLine($"        angle_delta = t * {rotateMod.Speed} * (math.pi / 180.0)");
                            sb.AppendLine($"        obj.rotation_euler[{axisIndex}] = start_angle + angle_delta");
                            sb.AppendLine($"        obj.keyframe_insert(data_path='rotation_euler', index={axisIndex}, frame=f)");
                        }
                    }
                }
            }
        }

        // Fallback Light if none exists
        if (!hasLights)
        {
            sb.AppendLine("# Default Fallback Light");
            sb.AppendLine("bpy.ops.object.light_add(type='SUN', location=(5, -5, 10))");
            sb.AppendLine("bpy.context.object.data.energy = 5");
        }

        // Camera Setup
        sb.AppendLine("# Camera Setup");
        sb.AppendLine("bpy.ops.object.camera_add(location=(0, -10, 5), rotation=(1.1, 0, 0))");
        sb.AppendLine("cam = bpy.context.object");
        sb.AppendLine("bpy.context.scene.camera = cam");
        
        // Auto-Frame Logic
        if (world != null && !world.AutoCamera)
        {
            sb.AppendLine($"# Manual Camera Setup");
            // Spherical coordinates
            sb.AppendLine($"dist = {world.CameraDistance}");
            sb.AppendLine($"height = {world.CameraHeight}");
            sb.AppendLine($"angle = math.radians({world.CameraAngle})");
            
            sb.AppendLine($"x = dist * math.sin(angle)");
            sb.AppendLine($"y = -dist * math.cos(angle)");
            sb.AppendLine($"z = height");
            
            sb.AppendLine($"cam.location = (x, y, z)");
            
            // Look at origin (0,0,0) or some center
            sb.AppendLine($"bpy.ops.object.empty_add(location=(0, 0, 0))");
            sb.AppendLine($"target = bpy.context.object");
            sb.AppendLine($"bpy.context.view_layer.objects.active = cam");
            sb.AppendLine($"bpy.ops.object.constraint_add(type='TRACK_TO')");
            sb.AppendLine($"cam.constraints['Track To'].target = target");
            sb.AppendLine($"cam.constraints['Track To'].track_axis = 'TRACK_NEGATIVE_Z'");
            sb.AppendLine($"cam.constraints['Track To'].up_axis = 'UP_Y'");
        }
        else
        {
            sb.AppendLine("min_x, min_y, min_z = float('inf'), float('inf'), float('inf')");
            sb.AppendLine("max_x, max_y, max_z = float('-inf'), float('-inf'), float('-inf')");
            sb.AppendLine("has_mesh = False");
            sb.AppendLine("for obj in bpy.context.scene.objects:");
            sb.AppendLine("    if obj.type == 'MESH':");
            sb.AppendLine("        has_mesh = True");
            sb.AppendLine("        for v in obj.bound_box:");
            sb.AppendLine("            world_v = obj.matrix_world @ mathutils.Vector(v)");
            sb.AppendLine("            min_x = min(min_x, world_v.x)");
            sb.AppendLine("            min_y = min(min_y, world_v.y)");
            sb.AppendLine("            min_z = min(min_z, world_v.z)");
            sb.AppendLine("            max_x = max(max_x, world_v.x)");
            sb.AppendLine("            max_y = max(max_y, world_v.y)");
            sb.AppendLine("            max_z = max(max_z, world_v.z)");
            
            sb.AppendLine("if has_mesh:");
            sb.AppendLine("    center_x = (min_x + max_x) / 2");
            sb.AppendLine("    center_y = (min_y + max_y) / 2");
            sb.AppendLine("    center_z = (min_z + max_z) / 2");
            sb.AppendLine("    size = max(max_x - min_x, max_y - min_y, max_z - min_z)");
            sb.AppendLine("    print(f'DEBUG: Center: ({center_x}, {center_y}, {center_z}), Size: {size}')");
            sb.AppendLine("    # Position camera to look at center");
            sb.AppendLine("    dist = size * 1.5 if size > 0 else 10");
            sb.AppendLine("    cam.location = (center_x, center_y - dist, center_z + (dist * 0.5))");
            sb.AppendLine("    print(f'DEBUG: Camera Location: {cam.location}')");
            sb.AppendLine("    # Add Track To constraint");
            sb.AppendLine("    bpy.ops.object.empty_add(location=(center_x, center_y, center_z))");
            sb.AppendLine("    target = bpy.context.object");
            sb.AppendLine("    bpy.context.view_layer.objects.active = cam");
            sb.AppendLine("    bpy.ops.object.constraint_add(type='TRACK_TO')");
            sb.AppendLine("    cam.constraints['Track To'].target = target");
            sb.AppendLine("    cam.constraints['Track To'].track_axis = 'TRACK_NEGATIVE_Z'");
            sb.AppendLine("    cam.constraints['Track To'].up_axis = 'UP_Y'");
        }

        // Render Settings
        sb.AppendLine("# Render Settings");
        
        // Use configured render engine
        string engine = videoSettings.RenderEngine;
        if (string.IsNullOrEmpty(engine)) engine = "BLENDER_EEVEE";
        sb.AppendLine($"bpy.context.scene.render.engine = '{engine}'"); 
        
        // Engine specific settings
        if (engine == "BLENDER_EEVEE")
        {
             sb.AppendLine("if hasattr(bpy.context.scene, 'eevee'):");
             sb.AppendLine("    bpy.context.scene.eevee.taa_render_samples = 64"); 
        }
        else if (engine == "CYCLES")
        {
             sb.AppendLine("bpy.context.scene.cycles.device = 'GPU'");
             sb.AppendLine("bpy.context.scene.cycles.samples = 128"); 
        }

        // Resolution & FPS
        sb.AppendLine($"bpy.context.scene.render.resolution_x = {videoSettings.ResolutionWidth}");
        sb.AppendLine($"bpy.context.scene.render.resolution_y = {videoSettings.ResolutionHeight}");
        sb.AppendLine("bpy.context.scene.render.resolution_percentage = 100");
        sb.AppendLine($"bpy.context.scene.render.fps = {videoSettings.FrameRate}");
        
        sb.AppendLine($"bpy.context.scene.render.filepath = '{outputFile.Replace("\\", "/")}'");
        
        if (isPreview)
        {
            sb.AppendLine("bpy.context.scene.render.image_settings.file_format = 'PNG'");
            sb.AppendLine("bpy.context.scene.frame_current = 0");
            sb.AppendLine("bpy.ops.render.render(write_still=True)");
        }
        else
        {
            sb.AppendLine("# Video Output Settings");
            sb.AppendLine("is_video_configured = False");
            sb.AppendLine("try:");
            sb.AppendLine("    # Try standard FFMPEG format");
            sb.AppendLine("    bpy.context.scene.render.image_settings.file_format = 'FFMPEG'");
            sb.AppendLine("    is_video_configured = True");
            sb.AppendLine("except Exception as e:");
            sb.AppendLine("    print(f'Warning: Standard FFMPEG format not available: {e}')");
            sb.AppendLine("    try:");
            sb.AppendLine("        # Try Blender 5.0+ specific media_type approach");
            sb.AppendLine("        bpy.context.scene.render.image_settings.file_format = 'PNG'");
            sb.AppendLine("        # Check if media_type exists before setting");
            sb.AppendLine("        if hasattr(bpy.context.scene.render.image_settings, 'media_type'):");
            sb.AppendLine("            bpy.context.scene.render.image_settings.media_type = 'VIDEO'");
            sb.AppendLine("        else:");
            sb.AppendLine("            print('Warning: media_type property not found on image_settings. Attempting to set anyway as dynamic property...')");
            sb.AppendLine("            try: bpy.context.scene.render.image_settings.media_type = 'VIDEO'");
            sb.AppendLine("            except: print('Failed to set media_type.')");
            
            sb.AppendLine("        is_video_configured = True");
            sb.AppendLine("    except Exception as e2:");
            sb.AppendLine("        print(f'Error: Could not configure video output: {e2}')");
            sb.AppendLine("        try:");
            sb.AppendLine("            formats = [i.identifier for i in bpy.context.scene.render.image_settings.bl_rna.properties['file_format'].enum_items]");
            sb.AppendLine("            print(f'Available formats: {formats}')");
            sb.AppendLine("        except: pass");
            sb.AppendLine("        raise e2");

            sb.AppendLine("if is_video_configured:");
            sb.AppendLine("    try:");
            if (format == "AVI")
                sb.AppendLine("        bpy.context.scene.render.ffmpeg.format = 'AVI'");
            else if (format == "MKV")
                sb.AppendLine("        bpy.context.scene.render.ffmpeg.format = 'MATROSKA'");
            else // MP4
                sb.AppendLine("        bpy.context.scene.render.ffmpeg.format = 'MPEG4'");

            string codec = "H264";
            if (videoSettings.Codec == "VP9") codec = "WEBM"; 
            else if (videoSettings.Codec == "H.265") codec = "H265"; // Support H.265
            
            sb.AppendLine($"        bpy.context.scene.render.ffmpeg.codec = '{codec}'");

            // Bitrate Control
            if (videoSettings.BitrateMode == "CBR")
            {
                sb.AppendLine("        bpy.context.scene.render.ffmpeg.constant_rate_factor = 'NONE'");
                sb.AppendLine($"        bpy.context.scene.render.ffmpeg.video_bitrate = {videoSettings.BitrateKbps}");
                sb.AppendLine($"        bpy.context.scene.render.ffmpeg.min_video_bitrate = {videoSettings.BitrateKbps}");
                sb.AppendLine($"        bpy.context.scene.render.ffmpeg.max_video_bitrate = {videoSettings.BitrateKbps}");
            }
            else
            {
                sb.AppendLine("        bpy.context.scene.render.ffmpeg.constant_rate_factor = 'MEDIUM'");
                sb.AppendLine($"        bpy.context.scene.render.ffmpeg.video_bitrate = {videoSettings.BitrateKbps}");
            }
            
            sb.AppendLine($"        bpy.context.scene.render.ffmpeg.gopsize = {videoSettings.GopSize}");
            sb.AppendLine("        bpy.context.scene.render.ffmpeg.ffmpeg_preset = 'GOOD'");
            sb.AppendLine("    except Exception as e_ffmpeg:");
            sb.AppendLine("        print(f'Error setting FFMPEG parameters: {e_ffmpeg}')");
            sb.AppendLine("        # Continue anyway, as defaults might work");

            sb.AppendLine("print(f'DEBUG: Output Format: {bpy.context.scene.render.image_settings.file_format}')");
            sb.AppendLine("print(f'DEBUG: Output Path: {bpy.context.scene.render.filepath}')");
            
            sb.AppendLine($"bpy.context.scene.frame_end = {frameEnd}");
            sb.AppendLine("bpy.ops.render.render(animation=True)");
        }

        return sb.ToString();
    }

    private Process? _blenderProcess;
    private const int ServerPort = 5000;

    Task IRenderService.InitializeAsync() => InitializeAsync();

    private async Task InitializeAsync()
    {
        var blenderPath = _configService.Config.BlenderPath;
        if (string.IsNullOrEmpty(blenderPath) || !File.Exists(blenderPath))
        {
            throw new FileNotFoundException("Blender executable not found.");
        }

        var scriptPath = Path.Combine(Path.GetTempPath(), "blender_server.py");
        // Simple Python TCP Server script to execute commands
        var pyScript = $@"
import bpy
import socket
import struct
import sys

# Flush stdout to ensure logs are captured immediately
sys.stdout.reconfigure(line_buffering=True)

try:
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(('127.0.0.1', {ServerPort}))
    server.listen(1)
    print('Blender Server Listening on {ServerPort}')

    while True:
        client = None
        try:
            client, addr = server.accept()
            header = client.recv(4)
            if not header:
                client.close()
                continue
                
            size = struct.unpack('<I', header)[0]
            data = b''
            while len(data) < size:
                packet = client.recv(4096)
                if not packet: break
                data += packet
                
            script = data.decode('utf-8')
            response = 'OK'
            try:
                # Execute the script
                # Note: This runs in the main thread and blocks
                exec(script)
            except Exception as e:
                response = str(e)
                print(f'Script Error: {{e}}')
                
            resp_data = response.encode('utf-8')
            client.send(struct.pack('<I', len(resp_data)))
            client.send(resp_data)
        except Exception as e:
            print(f'Server Loop Error: {{e}}')
        finally:
            if client:
                try: client.close()
                except: pass
except Exception as main_e:
    print(f'Critical Server Error: {{main_e}}')
";

        await File.WriteAllTextAsync(scriptPath, pyScript);

        var startInfo = new ProcessStartInfo
        {
            FileName = blenderPath,
            Arguments = $"--background --python \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _blenderProcess = Process.Start(startInfo);
        if (_blenderProcess != null)
        {
            ChildProcessTracker.AddProcess(_blenderProcess);
        }
        // Give Blender some time to initialize the python script and socket
        await Task.Delay(2000);
    }

    public void Shutdown()
    {
        if (_blenderProcess != null && !_blenderProcess.HasExited)
        {
            _blenderProcess.Kill();
            _blenderProcess = null;
        }
    }
}
