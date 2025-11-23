# Hardwareless - Procedural Music System

[![Unity Version](https://img.shields.io/badge/Unity-2021.3.22f1-blue.svg)](https://unity3d.com/get-unity/download/archive)
[![.NET](https://img.shields.io/badge/.NET-9.0.307-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 🎵 Advanced Unity Game with Procedural Music System & Metahuman Character Creation

**Hardwareless** is a cutting-edge Unity 3D game featuring an innovative procedural music system that adapts dynamically to gameplay, plus a comprehensive Metahuman character creation system for creating highly customizable characters. The music system includes real-time synthesis, adaptive audio cues, and an enhanced debug HUD with countdown timers and save notifications.

## ✨ Key Features

### 🎼 Procedural Music System
- **Real-time Audio Synthesis** - Dynamic music generation based on game state
- **Adaptive Progression** - Auto-advancing musical sections with customizable timing
- **Enhanced HUD** - Countdown display and "Saved" toast notifications
- **Persistent Settings** - Save/load music configurations
- **Debug Controls** - F9 key toggles comprehensive debug interface

### 🎭 Metahuman Character Creation
- **Comprehensive Customization** - Body, face, eyes, nose, mouth, ears, colors
- **Preset System** - 10 save slots with JSON import/export
- **Smooth Transitions** - Real-time morphing between character configurations
- **Debug HUD** - F10 key toggles character creator interface
- **Audio Integration** - UI sound effects for save/load/randomize actions

### 🎮 Game Features
- **Unity 2021.3.22f1** - Latest LTS Unity engine
- **Advanced Audio Pipeline** - Custom audio system with SFX library
- **Modular Architecture** - Clean separation of concerns with assembly definitions
- **Development Tools** - Comprehensive setup scripts and documentation

## 🚀 Quick Start

### Prerequisites
- Unity 2021.3.22f1 (exact version required)
- .NET SDK 9.0.307
- Windows Build Support (IL2CPP)
- Git with LFS support

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/dercel74/hardwareless-procedural-music.git
   cd hardwareless-procedural-music
   ```

2. **Install Unity 2021.3.22f1:**
   - Run `install-unity.ps1` for automated installation
   - Or follow `UNITY_DIREKTINSTALL.md` for manual setup

3. **Open in Unity:**
   - Unity Hub → Projects → Add project from disk
   - Select the cloned repository folder
   - Unity will automatically configure the project

4. **Test the Music System:**
   - Press Play in Unity Editor
   - Press `F9` to open the music debug HUD
   - Explore countdown timers and auto-save features

5. **Test the Character Creator:**
   - Press `F10` to open the character creation HUD
   - Use sliders to customize character appearance
   - Try randomize, presets, and color customization

## 📚 Documentation

- **[Setup Guide](SETUP_GUIDE.md)** - Complete development environment setup
- **[Unity Installation](UNITY_DIREKTINSTALL.md)** - Step-by-step Unity setup
- **[Project Status](PROJECT_STATUS.md)** - Overview and quick reference
- **[Procedural Music](Assets/Documentation/ProceduralMusic.md)** - Complete music system reference
- **[Metahuman Character Creation](Assets/Documentation/MetahumanCharacterCreation.md)** - Character creator system reference
- **[Quickstart](QUICKSTART.md)** - Development summary and statistics

## 🏗️ Project Structure

```
hardwareless/
├── Assets/
│   ├── Scripts/
│   │   ├── Audio/               # Procedural music system
│   │   │   ├── ProceduralMusic.cs          (50.1 KB)
│   │   │   ├── ProceduralMusicManager.cs   (64.3 KB)
│   │   │   └── ProceduralMusicDebugHUD.cs  (30.7 KB)
│   │   └── Character/           # Metahuman character creation
│   │       ├── CharacterData.cs
│   │       ├── CharacterCustomization.cs
│   │       ├── CharacterPresetManager.cs
│   │       ├── MetahumanCharacterCreator.cs
│   │       └── CharacterCreatorDebugHUD.cs
│   ├── Documentation/
│   │   └── ProceduralMusic.md   # Complete system documentation
│   └── Scenes/                  # Game scenes and levels
├── .vscode/                     # VS Code integration
├── .githooks/                   # Custom git hooks
└── Documentation/               # Setup and development guides
```

## 🎯 Music System Highlights

### Enhanced Debug HUD
- **Auto-Progression Countdown** - Visual timer showing next music progression
- **Save Toast Notifications** - Temporary "Saved" indicators for user feedback
- **Real-time Controls** - Live music parameter adjustment
- **Performance Metrics** - Audio pipeline statistics

### Technical Architecture
- **147+ KB of music code** - Comprehensive procedural audio system
- **6 Core Components** - Modular, maintainable architecture
- **Assembly Definitions** - Clean compilation boundaries
- **Event-Driven Design** - Responsive to game state changes

## 🔧 Development Environment

### Configured Tools
- **.NET SDK 9.0.307** - Latest C# features and performance
- **Python (uv)** - Modern package management for tools
- **Git + LFS** - Large file support for Unity assets
- **EditorConfig** - Consistent code formatting
- **VS Code Integration** - IntelliSense and debugging

### Git Workflow
- **Main Branch** - Stable, production-ready code
- **Development Branch** - Integration branch for features
- **Feature Branches** - Individual feature development
- **Automated Hooks** - Pre-commit validation and LFS integration

## 📊 Project Statistics

- **16 Commits** - Well-documented development history
- **3 Active Branches** - Organized development workflow
- **700+ Lines Documentation** - Comprehensive guides and references
- **Zero-Config Setup** - Automated installation and configuration

## 🎮 Getting Started with Music System

1. **Open Unity and enter Play mode**
2. **Press F9** to open the procedural music debug HUD
3. **Observe the countdown timer** showing next auto-progression
4. **Test save functionality** - watch for "Saved" toast notifications
5. **Experiment with parameters** - adjust music generation settings
6. **Explore documentation** - `Assets/Documentation/ProceduralMusic.md`

## 🎭 Getting Started with Character Creator

1. **Open Unity and enter Play mode**
2. **Press F10** to open the character creation debug HUD
3. **Click "Randomize"** to generate random character appearances
4. **Navigate tabs** - Body, Face, Colors, Presets
5. **Adjust sliders** - Real-time character customization
6. **Save/load presets** - Try the 10 preset slots
7. **Explore documentation** - `Assets/Documentation/MetahumanCharacterCreation.md`

## 🤝 Contributing

This project follows a professional development workflow:

- Create feature branches from `development`
- Submit pull requests for code review
- Follow C# coding guidelines in `CODING_GUIDELINES.md`
- Test thoroughly before merging

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🔗 Links

- **Repository**: https://github.com/dercel74/hardwareless-procedural-music
- **Unity Version**: [2021.3.22f1](https://unity3d.com/get-unity/download/archive)
- **Documentation**: Complete guides included in repository

---

**Ready to experience dynamic procedural music in Unity!** 🎵✨
