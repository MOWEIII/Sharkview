# AutoRenderer

A modern, cross-platform automated rendering tool built with **C#** and **Avalonia UI**, designed to orchestrate **Blender** for rendering scenes and generating videos programmatically.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)

## ✨ Key Features

*   **🎬 Scene Editor**:
    *   **Import Models**: Supports **FBX, OBJ, GLB, STL** formats.
    *   **Live Preview**: Real-time 3D preview powered by a persistent Blender backend for instant feedback.
    *   **Scene Graph**: Manage lights and objects in a hierarchical view.
    *   **Properties Panel**: Precise control over Position, Rotation (Euler XYZ), and Scale.
    *   **Modifiers**: Add procedural animations like **Auto-Rotate** directly from the UI.

*   **🎥 Rendering**:
    *   **Batch Processing**: Generates Python automation scripts to drive Blender in the background.
    *   **Format Support**: Exports to **MP4, AVI, MKV**, or falls back to **PNG Sequences** if video codecs are unavailable.
    *   **Console Output**: Real-time streaming of Blender logs to the built-in Console for debugging.

*   **🛠️ Tools**:
    *   **Console Tab**: dedicated system log for monitoring render progress and errors.
    *   **Settings**: Auto-detection of Blender installation and customizable output paths.

## 🚀 Getting Started

### Prerequisites
*   **Blender 5.0+** (Tested with 5.0).
*   **.NET 9.0 SDK**.

### Installation & Run

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/yourusername/AutoRenderer.git
    cd AutoRenderer
    ```

2.  **Build**:
    ```bash
    dotnet build
    ```

3.  **Run**:
    ```bash
    dotnet run --project src/AutoRenderer/AutoRenderer.csproj
    ```

## 📖 User Guide

### 1. ⚙️ Configuration
Upon first launch, navigate to the **Settings** tab:
*   **Blender Path**: Click "Auto Detect" or manually browse to your `blender.exe`.
*   **Output Directory**: Choose where your rendered videos/images will be saved.

### 2. 🏠 Scene Composition (Editor)
*   **Import**: Click "Import Model" to load your 3D assets.
*   **Camera**:
    *   **Auto-Center**: Automatically keeps your object in frame.
    *   **Manual Control**: Uncheck "Auto-Center" to set specific Distance, Height, and Angle.
*   **Preview**: Toggle "Auto-Refresh" to see changes instantly, or click "Refresh" manually.

### 3. 🎬 Rendering
*   Go to the **Render** tab.
*   Set your **Output Filename**, **Duration** (seconds), and **Resolution**.
*   Click **Start Rendering**.
*   Switch to the **Console** tab to watch the progress.

## 🏗️ Architecture

The solution follows a clean **MVVM** architecture:

*   **`src/AutoRenderer`**: The UI layer using Avalonia XAML.
*   **`src/AutoRenderer.Core`**: Business logic and Services.
    *   `RenderService`: Manages the persistent Blender process and socket communication.
    *   `BlenderService`: Handles path detection.
    *   `ConsoleService`: Centralized logging system.
*   **`tests/AutoRenderer.Tests`**: XUnit test suite.

## 🤝 Contributing

Contributions are welcome! Please fork the repository and submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
