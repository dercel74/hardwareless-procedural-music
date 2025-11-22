<!-- markdownlint-disable MD031 MD040 MD032 -->
# 🎮 Hardwareless - Development Environment Setup

## Prerequisites & Toolchain Setup

This guide covers setting up the complete development environment for the Unity Hardwareless project with tactical combat features and procedural music system.

**Target Audience**: Developers implementing the tactical combat system and audio features
**Time Required**: 30-60 minutes for full setup
**Requirements**: Windows 10/11, Administrator privileges for some installations

---

## 📋 Table of Contents

0. [Development Environment Setup](#0-development-environment-setup)
1. [Unity Project Setup](#1-unity-projekt-setup)
2. [Layer & Tags Configuration](#2-layer--tags-konfiguration)
3. [NavMesh Setup](#3-navmesh-setup)
4. [Enemy AI Setup](#4-enemy-ai-setup)
5. [Player Setup (Placeholder)](#5-player-setup-placeholder)
6. [Testing & Debugging](#6-testing--debugging)
7. [Performance Optimization](#7-performance-optimierung)

---

## 0. Development Environment Setup

### Step 0.1: .NET SDK Installation

Install .NET SDK 9.0.306 for latest C# features:

```powershell
# Download and install .NET SDK 9.0.306
$script = "$env:TEMP\dotnet-install.ps1"
iwr https://dot.net/v1/dotnet-install.ps1 -UseBasicParsing -OutFile $script
& $script -Channel 9.0 -Version 9.0.306 -InstallDir "$HOME\.dotnet"

# Add to PATH for this session
$env:PATH = "$HOME\.dotnet;$HOME\.dotnet\tools;$env:PATH"

# Verify installation
dotnet --version  # Should show 9.0.306
```

### Step 0.2: Python Toolchain (uv)

Install modern Python package manager:

```powershell
# Install uv via PowerShell
irm https://astral.sh/uv/install.ps1 | iex

# Add to PATH for this session
$env:Path = "$HOME\.local\bin;$env:Path"

# Verify installation
uv --version  # Should show latest version
```

### Step 0.3: Git & Git LFS

Install Git LFS for handling large Unity assets:

```powershell
# Install Git LFS via winget
winget install --id GitHub.GitLFS -e --source winget

# Initialize LFS in the repository
git lfs install
git lfs --version  # Verify installation
```

### Step 0.4: Unity Hub & Editor

1. **Install Unity Hub**:
   - Download from [Unity website](https://unity.com/download) or use winget if available
   - `winget search Unity` to find available packages

2. **Install Unity Editor 2021.3.22f1**:
   - Open Unity Hub
   - Go to "Installs" tab
   - Click "Add" → "Add from Archive" or "Install Editor"
   - Select version **2021.3.22f1** (specific version required by project)
   - Include modules: Windows Build Support, Visual Studio integration

### Step 0.5: VS Code Setup (Optional)

Install recommended VS Code extensions:

```powershell
# Install VS Code extensions via command line (if VS Code is in PATH)
code --install-extension ms-dotnettools.csdevkit
code --install-extension Unity.unity-debug
code --install-extension ms-vscode.vscode-json
code --install-extension editorconfig.editorconfig
```

### Step 0.6: Verify Environment

```powershell
# Check all tools are available
dotnet --version    # 9.0.306
uv --version        # Latest uv version
git lfs --version   # Git LFS version
git --version       # Git version

# Navigate to project directory
cd "C:\path\to\hardwareless"

# Verify Unity project structure
ls Assets, ProjectSettings, Packages  # Should exist
```

---

## 1. Unity Projekt-Setup

### Schritt 1.1: Scene erstellen

1. **Neue Scene erstellen**:
   - `File > New Scene > Basic (Built-in)`
   - `Speichern als`: `Prototype_Combat.unity` in `Assets/Scenes/`

2. **Basis-Geometrie**:
   ```
   GameObject > 3D Object > Plane
   Name: Ground
   Transform: Position (0, 0, 0), Scale (5, 1, 5)
   ```

3. **Beleuchtung**:
   ```
   Directional Light (Standard vorhanden)
   Intensity: 1
   ```

### Schritt 1.2: Kamera Setup (Top-Down)

```
Main Camera:
Position: (0, 20, -10)
Rotation: (60, 0, 0)
Projection: Perspective
Field of View: 60
```

### Schritt 1.3: Scripts verifizieren

Prüfe dass folgende Scripts vorhanden sind:
- ✅ `Assets/Scripts/Systems/Combat/VisionCone.cs`
- ✅ `Assets/Scripts/Systems/Combat/HearingSystem.cs`
- ✅ `Assets/Scripts/Systems/Combat/EnemyController.cs`
- ✅ `Assets/Scripts/Systems/Combat/Damageable.cs`
- ✅ `Assets/Scripts/Systems/Combat/ProjectileBase.cs`
- ✅ `Assets/Scripts/Systems/Combat/CombatLogger.cs`

---

## 2. Layer & Tags Konfiguration

### Schritt 2.1: Layers anlegen

`Edit > Project Settings > Tags and Layers`

**Neue Layers**:
```
Layer 6:  Player
Layer 7:  Enemy
Layer 8:  Obstacles
Layer 9:  Cover
Layer 10: Projectile
```

### Schritt 2.2: Tags anlegen

**Neue Tags**:
```
Tag: Player
Tag: Enemy
Tag: Cover
Tag: Patrol Point
```

### Schritt 2.3: LayerMask verstehen

**Wichtig für Vision & Hearing**:
- `targetMask`: Was kann erkannt werden (Player/Enemy)
- `obstacleMask`: Was blockiert Sicht/Sound (Obstacles)
- `noiseMask`: Was kann Geräusche machen (Player/Enemy)

---

## 3. NavMesh Setup

### Schritt 3.1: NavMesh Bake

1. **Navigation Window öffnen**:
   ```
   Window > AI > Navigation
   ```

2. **Bake Settings**:
   ```
   Agent Radius: 0.5
   Agent Height: 2
   Max Slope: 45
   Step Height: 0.4
   ```

3. **Ground Object markieren**:
   ```
   Ground Plane auswählen
   Inspector > Navigation (Static) ✓ Walkable
   ```

4. **Bake**:
   ```
   Navigation Window > Bake Tab > "Bake" Button
   ```

**Resultat**: Blaue NavMesh-Fläche sichtbar in Scene View

### Schritt 3.2: Hindernisse hinzufügen

```
GameObject > 3D Object > Cube
Name: Wall_1
Scale: (0.5, 2, 5)
Layer: Obstacles
Navigation: Not Walkable ✓
```

**Bake erneut** nach Hinzufügen von Hindernissen!

---

## 4. Enemy AI Setup

### Schritt 4.1: Enemy Prefab erstellen

1. **Basis-GameObject**:
   ```
   GameObject > 3D Object > Capsule
   Name: Enemy_Base
   Position: (0, 1, 0)
   Layer: Enemy
   Tag: Enemy
   ```

2. **Components hinzufügen**:

   **NavMeshAgent**:
   ```
   Add Component > NavMeshAgent
   Speed: 3.5
   Angular Speed: 120
   Acceleration: 8
   Stopping Distance: 0.5
   Auto Braking: ✓
   ```

   **EnemyController**:
   ```
   Add Component > EnemyController
   (Fügt automatisch VisionCone & HearingSystem hinzu!)
   ```

3. **EnemyController konfigurieren**:

   **Combat Settings**:
   ```
   Attack Range: 15
   Attack Damage: 20
   Fire Rate: 1
   Projectile Prefab: [Zu erstellen - siehe 4.3]
   Weapon Muzzle: [Auto-generiert oder manuell zuweisen]
   ```

   **Patrol Settings**:
   ```
   Patrol Points: [Zu erstellen - siehe 4.2]
   Patrol Wait Time: 2
   Patrol Speed: 2
   ```

   **Alert Settings**:
   ```
   Alert Speed: 4
   Search Duration: 10
   Search Radius: 10
   ```

   **Cover Settings**:
   ```
   Use Cover: ✓
   Min Combat Distance: 8
   ```

4. **VisionCone konfigurieren** (Auto-added):
   ```
   View Angle: 90
   View Distance: 20
   Detection Interval: 0.2
   Target Mask: Player
   Obstacle Mask: Obstacles
   Use Light Detection: ✓
   Use Cover Detection: ✓
   Show Debug Gizmos: ✓
   ```

5. **HearingSystem konfigurieren** (Auto-added):
   ```
   Max Hearing Range: 25
   Hearing Threshold: 0.1
   Detection Interval: 0.3
   Noise Mask: Player
   Sound Obstacle Mask: Obstacles
   Use Material Damping: ✓
   Obstacle Damping: 0.7
   Show Debug Gizmos: ✓
   ```

6. **Damageable hinzufügen**:
   ```
   Add Component > Damageable
   Max Health: 100
   Armor Value: 0
   Is Invulnerable: ✗
   ```

7. **Visuelles Feedback** (Optional):
   ```
   Material mit Farbe erstellen (z.B. Rot)
   Auf Capsule Renderer ziehen
   ```

### Schritt 4.2: Patrol Points erstellen

1. **Parent Object**:
   ```
   Create Empty GameObject
   Name: PatrolRoute_1
   Position: (0, 0, 0)
   ```

2. **Waypoints erstellen**:
   ```
   Create Empty > Als Child von PatrolRoute_1
   Name: Waypoint_1
   Position: (-5, 0, 0)
   Tag: Patrol Point

   Wiederholen für Waypoint_2, Waypoint_3, etc.
   Beispiel-Route:
   - Waypoint_1: (-5, 0, 0)
   - Waypoint_2: (5, 0, 0)
   - Waypoint_3: (5, 0, 10)
   - Waypoint_4: (-5, 0, 10)
   ```

3. **Gizmos visualisieren**:
   ```
   Jedes Waypoint GameObject auswählen
   Add Component > Empty script (nur für Gizmo-Icon)
   Oder: Icon im Inspector setzen
   ```

4. **Waypoints zuweisen**:
   ```
   Enemy_Base auswählen
   EnemyController > Patrol Points (Array)
   Size: 4
   Element 0: Waypoint_1
   Element 1: Waypoint_2
   Element 2: Waypoint_3
   Element 3: Waypoint_4
   ```

### Schritt 4.3: Projectile Prefab erstellen

1. **Projectile GameObject**:
   ```
   GameObject > 3D Object > Sphere
   Name: Bullet_Basic
   Scale: (0.1, 0.1, 0.1)
   Layer: Projectile
   ```

2. **Components**:

   **Rigidbody**:
   ```
   Add Component > Rigidbody
   Mass: 0.01
   Drag: 0
   Angular Drag: 0
   Use Gravity: ✗
   Is Kinematic: ✗
   Collision Detection: Continuous Dynamic
   ```

   **Sphere Collider**:
   ```
   Is Trigger: ✗
   Radius: 0.5
   ```

   **ProjectileBase**:
   ```
   Add Component > ProjectileBase
   Speed: 80
   Max Lifetime: 5
   Damage: 15
   Hit Effect: [Optional - Particle System]
   ```

3. **Material** (Optional):
   ```
   Gelbes/Oranges emissive Material
   ```

4. **Prefab speichern**:
   ```
   Bullet_Basic aus Hierarchy in Assets/Prefabs/ ziehen
   Aus Hierarchy löschen
   ```

5. **Enemy zuweisen**:
   ```
   Enemy_Base > EnemyController
   Projectile Prefab: Bullet_Basic (aus Assets/Prefabs/)
   ```

### Schritt 4.4: Enemy Prefab speichern

```
Enemy_Base aus Hierarchy in Assets/Prefabs/ ziehen
Name: Enemy_Patroller
```

---

## 5. Player Setup (Placeholder)

### Schritt 5.1: Basic Player Object

1. **GameObject erstellen**:
   ```
   GameObject > 3D Object > Capsule
   Name: Player
   Position: (0, 1, 5)
   Layer: Player
   Tag: Player
   ```

2. **Material** (Unterscheidung):
   ```
   Blaues Material erstellen und zuweisen
   ```

3. **NoiseEmitter hinzufügen**:
   ```
   Add Component > NoiseEmitter

   Walk Noise Level: 0.2
   Sprint Noise Level: 0.8
   Crouch Noise Level: 0.05
   Shooting Noise Level: 1.0
   Noise Duration: 0.5
   ```

4. **Damageable hinzufügen**:
   ```
   Add Component > Damageable
   Max Health: 100
   ```

### Schritt 5.2: Temporäre Movement (Test)

**Simpler Script für Testing**:

```csharp
// PlayerMovementTest.cs
using UnityEngine;

public class PlayerMovementTest : MonoBehaviour
{
    public float moveSpeed = 5f;
    private NoiseEmitter noise;

    void Start()
    {
        noise = GetComponent<NoiseEmitter>();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v) * moveSpeed * Time.deltaTime;
        transform.position += move;

        // Noise basierend auf Bewegung
        if (move.magnitude > 0.01f)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                noise?.SetMovementNoise(MovementState.Sprinting);
            else if (Input.GetKey(KeyCode.LeftControl))
                noise?.SetMovementNoise(MovementState.Crouching);
            else
                noise?.SetMovementNoise(MovementState.Walking);
        }
        else
        {
            noise?.SetMovementNoise(MovementState.Idle);
        }

        // Test: Shoot Noise
        if (Input.GetKeyDown(KeyCode.Space))
        {
            noise?.MakeNoise(1f); // Loud!
        }
    }
}
```

**Hinzufügen**:
```
Player > Add Component > PlayerMovementTest
```

---

## 6. Testing & Debugging

### Schritt 6.1: Initial Test

1. **Play Mode starten**
2. **Beobachten**:
   - Enemy patrouilliert zwischen Wegpunkten
   - Vision Cone (grüner Gizmo) sichtbar
   - Hearing Range (blauer Gizmo) sichtbar

3. **Player bewegen**:
   - WASD Tasten
   - Enemy sollte Player erkennen (roter Vision Cone)
   - Enemy State wechselt zu "Combat"

4. **Noise testen**:
   - `Shift` halten = Sprint (laut)
   - `Ctrl` halten = Crouch (leise)
   - `Space` = Shoot Noise

### Schritt 6.2: Debug-Features nutzen

**Scene View Gizmos**:
- ✅ Grüner Kegel = Idle Vision
- ✅ Roter Kegel = Target Detected
- ✅ Blaue Kugel = Hearing Range
- ✅ Gelbe Linien = Detected Noises
- ✅ Rote Linien = Visible Targets

**Console Logs prüfen**:
```
[VisionCone] Initialized on Enemy_Base
[HearingSystem] Initialized on Enemy_Base
[EnemyController] Enemy_Base initialized
[EnemyController] Enemy_Base: State changed Patrol → Combat
[CombatLog] Enemy_Base: Fired at target
```

**Log-Datei**:
```
Pfad: C:\Users\zande\Desktop\Hardwarelessasu\Docs\logs\combat_log.txt
```

### Schritt 6.3: Häufige Probleme

**Problem**: Enemy bewegt sich nicht
- ✅ NavMesh gebaked?
- ✅ NavMeshAgent vorhanden?
- ✅ Patrol Points zugewiesen?

**Problem**: Enemy erkennt Player nicht
- ✅ Player Layer = "Player"?
- ✅ VisionCone Target Mask = "Player"?
- ✅ Keine Obstacles im Weg?

**Problem**: Keine Logs erscheinen
- ✅ CombatLogger.cs vorhanden?
- ✅ Log-Pfad existiert?
- ✅ Console Logs aktiviert?

**Problem**: Projektile spawnen nicht
- ✅ Projectile Prefab zugewiesen?
- ✅ ProjectileBase Script vorhanden?
- ✅ Weapon Muzzle Transform gesetzt?

---

## 7. Performance-Optimierung

### Schritt 7.1: Update Intervals anpassen

**Bei vielen Enemies (>20)**:

```
VisionCone:
Detection Interval: 0.3 (Standard: 0.2)

HearingSystem:
Detection Interval: 0.4 (Standard: 0.3)
```

### Schritt 7.2: Range reduzieren

```
VisionCone:
View Distance: 15 (statt 20)

HearingSystem:
Max Hearing Range: 20 (statt 25)
```

### Schritt 7.3: Profiling

```
Window > Analysis > Profiler
Play Mode starten
CPU Usage > Scripts > Beobachten:
- VisionCone.DetectTargets
- HearingSystem.DetectNoise
- EnemyController.Update
```

**Zielwerte**:
- < 1ms pro Enemy bei 30 Enemies
- < 0.1ms bei einzelnem Enemy

---

## 🎯 Nächste Schritte


### Phase 1: Erweiterte Player-Steuerung

1. Richtigen PlayerController mit CharacterController
2. Kamera-System (Follow, Zoom)
3. Rotation/Aim-System


### Phase 2: UI Implementation

1. Health Bar
2. Detection Indicator
3. Noise Meter
4. Crosshair


### Phase 3: Combat Refinement

1. Weapon System (ScriptableObjects)
2. Cover-Mechanik
3. Reload/Ammo
4. Hit-Feedback

---

## ✅ Checkliste - Basis-Setup

- [ ] Scene erstellt und gespeichert
- [ ] Layers & Tags konfiguriert
- [ ] NavMesh gebaked
- [ ] Enemy Prefab erstellt
- [ ] VisionCone konfiguriert
- [ ] HearingSystem konfiguriert
- [ ] Patrol Points erstellt
- [ ] Projectile Prefab erstellt
- [ ] Player Placeholder erstellt
- [ ] NoiseEmitter auf Player
- [ ] Test durchgeführt
- [ ] Logs überprüft
- [ ] Debug Gizmos sichtbar

---

## 📚 Weitere Ressourcen

- **TACTICAL_SYSTEM_IMPLEMENTATION.md** - Vollständige Systemdokumentation
- **CODING_GUIDELINES.md** - Code-Standards
- **Unity NavMesh Docs**: <https://docs.unity3d.com/Manual/nav-BuildingNavMesh.html>
- **Combat Logs**: `Docs/logs/combat_log.txt`

---

**Version**: 1.0.0
**Letztes Update**: 2025-11-11
**Status**: ✅ Ready for Implementation
