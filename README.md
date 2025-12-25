# AutoRenderer

A modern, cross-platform automated rendering tool built with **C#** and **Avalonia UI**, designed to orchestrate **Blender** for rendering scenes and generating videos programmatically.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)

## ✨ Key Features

*   **🎬 Scene Editor**: Import models (**FBX, OBJ, GLB, STL**), real-time preview, and apply modifiers like **Auto-Rotate** (Frame-Rate Independent).
*   **🎥 Automated Rendering**: Batch render videos using **EEVEE** or **Cycles** with customizable encoding (H.264/H.265). Supports **Blender 5.0+**.
*   **⚡ Quick Actions**: Instantly **Save Image** or **Quick Render** directly from the editor.
*   **📊 Live Feedback**: Real-time status bar updates and integrated Blender log streaming.

## 📖 Usage

### 1. Configuration
Navigate to **Settings** to configure:
*   **Blender Path**: Auto-detect or manually set `blender.exe`.
*   **Render**: Choose Engine (EEVEE/Cycles), Resolution, Duration, and Output Directory.
*   **Video**: Set Codec and Bitrate.

### 2. Scene Composition
*   **Import**: Drag & Drop or select 3D models.
*   **Modifiers**: Add procedural animations (e.g., Rotation speed in °/s).
*   **Camera**: Use Auto-Center or manual controls.
*   **Render**: Click **Quick Render** to generate video based on global settings.

## 🏗️ Architecture

*   **UI**: Avalonia XAML (MVVM)
*   **Core**: C# .NET 9.0
*   **Backend**: Blender Python API (Socket Communication)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
