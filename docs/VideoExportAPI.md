# AutoRenderer Video Export API

This document describes the video export configuration parameters available in the AutoRenderer application.

## Configuration Object: `VideoExportSettings`

These settings are stored within the main `AppConfig` object under the `VideoSettings` property.

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `FrameRate` | `int` | `30` | Frames per second (FPS). Common values: 24, 30, 60. |
| `ResolutionWidth` | `int` | `1920` | Video width in pixels. |
| `ResolutionHeight` | `int` | `1080` | Video height in pixels. |
| `Codec` | `string` | `"H.264"` | Video compression format. Options: `"H.264"`, `"H.265"`, `"VP9"`. |
| `BitrateMode` | `string` | `"VBR"` | Bitrate control mode. Options: `"CBR"` (Constant), `"VBR"` (Variable). |
| `BitrateKbps` | `int` | `5000` | Target video bitrate in Kilobits per second (Kbps). |
| `GopSize` | `int` | `18` | Group of Pictures (GOP) size. Determines keyframe interval. |

## Usage in RenderService

The `RenderService` automatically applies these settings when generating the Blender Python script.

```csharp
// Example internal usage
var videoSettings = _configService.Config.VideoSettings;
bpy.context.scene.render.fps = videoSettings.FrameRate;
bpy.context.scene.render.resolution_x = videoSettings.ResolutionWidth;
// ...
```

### Blender Mapping

| AutoRenderer Setting | Blender Property | Notes |
|----------------------|------------------|-------|
| `FrameRate` | `render.fps` | Direct mapping. |
| `Resolution` | `render.resolution_x/y` | Scale is fixed at 100%. |
| `Codec` | `render.ffmpeg.codec` | "H.264"/"H.265" -> `'H264'`, "VP9" -> `'WEBM'` |
| `BitrateMode` | `render.ffmpeg.constant_rate_factor` | "CBR" sets this to `'NONE'`, "VBR" sets to `'MEDIUM'`. |
| `BitrateKbps` | `render.ffmpeg.video_bitrate` | Used for max/min in CBR, or target in VBR. |
| `GopSize` | `render.ffmpeg.gopsize` | Direct mapping. |

## User Interface

The Settings view provides a dedicated "Video Output Settings" panel:
- **Resolution Preset**: Quick selection for 720p, 1080p, 4K.
- **Custom Resolution**: Manual entry for width/height.
- **FPS & Codec**: Dropdown selection.
- **Bitrate Control**: Mode selection and numeric input (Kbps).
- **GOP**: Numeric input for keyframe interval.
