# 🎮 Rubik's Cube Simulator & Solver

A comprehensive interactive 3D Rubik's Cube simulator built with **C# and WPF**. This application provides a complete cube manipulation experience with real-time 3D visualization, keyboard and mouse controls, and an intelligent automatic solver using the beginner's layer-by-layer method.

## 🌟 Overview

This project is a fully functional Rubik's Cube application that simulates a real cube with accurate physics and movement mechanics. Users can interact with the cube in multiple ways, customize visual settings, and leverage a built-in solver to understand how to solve scrambled cubes using beginner-friendly techniques.

The application features:
- **3D Interactive Cube** rendered with WPF 3D graphics
- **Multiple Control Schemes** (keyboard, mouse, direct move input)
- **Automatic Solver** using the Layer-by-Layer (Beginner's) method
- **Advanced Visualization Settings** (lighting, gloss effects, animation speed)
- **Move Timer & Counter** to track solving progress
- **Six Different Perspective Views** (Green, Blue, Red, Orange, White, Yellow)

## 🚀 Key Features

### Interactive Controls
- **Keyboard Input**: Use standard Rubik's Cube notation (R, L, U, D, F, B, M moves with ', 2 modifiers)
- **Mouse Drag**: Click and drag on the cube to rotate it freely in 3D space
- **Directional Moves**: Arrow keys and face buttons to rotate the cube
- **Text Input Box**: Enter move sequences directly (e.g., "R U R' U'" for solving algorithms)

### 3D Visualization
- **Real-time 3D Rendering** using WPF Media3D
- **Dynamic Lighting System** with ambient, key, fill, and rim lights
- **Smooth Animations** for all cube rotations and moves
- **Customizable Visual Settings**:
  - Animation speed control
  - Gloss/Shininess adjustment
  - Lighting intensity controls
  - Cube piece gap adjustment
  - Rim lighting toggle

### Automatic Solving
- **Beginner's Method Solver** - Solves any scrambled cube using the 7-step layer-by-layer approach
- **Move Optimization** - Reduces unnecessary moves in the solution
- **Step-by-Step Solving Display** - Watch the solver work through each phase
- **Solution Verification** - Ensures the cube is properly solved after applying moves

### Additional Features
- **Cube State Tracking** - Displays move count and solving timer
- **Scramble Function** - Generate random cube scrambles
- **Reset Function** - Return cube to solved state instantly
- **Perspective Switching** - View the cube from different angles
- **Settings Persistence** - Save visual preferences between sessions

## 🛠️ Technology Stack

| Component | Technology |
|-----------|-----------|
| **Language** | C# 11 (.NET 8) |
| **GUI Framework** | Windows Presentation Foundation (WPF) |
| **3D Graphics** | WPF Media3D |
| **Build System** | MSBuild / Visual Studio |
| **Project Structure** | Multi-project solution |

## 📂 Project Structure

```
Rubik's Cube/
├── Rubik's Cube.sln          # Main solution file
├── README.md                  # This file
├── Rubik's Cube/              # Main WPF Application
│   ├── RubiksCube.cs          # Core cube data model and move logic
│   ├── BeginnersMethodSolver.cs # Automatic solver implementation
│   ├── MainWindow.xaml        # WPF UI layout definition
│   ├── MainWindow.xaml.cs     # UI logic, 3D rendering, animations
│   ├── App.xaml & App.xaml.cs # Application entry point
│   └── Properties/            # Project metadata and assembly info
├── Builder/                   # Utility project for geometry generation
│   └── Generator.cs           # Helper for creating 3D cube geometry
└── BevelGen/                  # Utility project for bevel generation
    └── Generator.cs           # Generates beveled edges for cube pieces
```

## 🎯 Core Components

### 1. **RubiksCube.cs** - The Data Model
Represents the internal state of a Rubik's Cube using six 3×3 color arrays (U, D, L, R, F, B), where each array corresponds to a face:
- **U (Up/Yellow)** - Top face
- **D (Down/White)** - Bottom face
- **L (Left/Orange)** - Left face
- **R (Right/Red)** - Right face
- **F (Front/Green)** - Front face
- **B (Back/Blue)** - Back face

Implements all standard Rubik's Cube moves:
- **Basic moves**: R, L, U, D, F, B (90° clockwise rotation)
- **Inverse moves**: R', L', U', D', F', B' (90° counter-clockwise)
- **Double moves**: R2, L2, U2, D2, F2, B2 (180° rotation)
- **Middle slice move**: M, M', M2

Each move is mathematically calculated by:
1. Rotating the target face 90° in the correct direction
2. Cycling the edge/corner pieces adjacent to that face

### 2. **BeginnersMethodSolver.cs** - The Solver Engine
Implements a Layer-by-Layer solving algorithm in 7 phases:

1. **White Cross** - Positions white edge pieces around the white center
2. **White Corners** - Places white corner pieces to complete the first layer
3. **Middle Layer** - Inserts second-layer edges without disturbing the first layer
4. **Yellow Cross** - Creates a yellow cross on the top layer
5. **Yellow Face** - Orients all yellow corner pieces to face upward
6. **Position Yellow Corners** - Moves yellow corners to their correct positions
7. **Position Yellow Edges** - Solves the final edges and completes the cube

Each phase uses logical pattern recognition and executes appropriate move sequences. The solver also includes move optimization to reduce solution length.

### 3. **MainWindow.xaml.cs** - The UI & 3D Engine
Handles all visual representation and user interaction:

**3D Rendering:**
- Builds a 3×3×3 cube from 26 individual cubie models (the center is hidden)
- Each cubie is a textured 3D box with colored sticker faces
- Uses 4 directional lights for realistic shading
- Applies smooth animations to rotation moves

**User Input:**
- Mouse drag detection and quaternion-based 3D rotation
- Keyboard input mapping with perspective-aware directional controls
- Move queue system to handle rapid input while animating
- Direct text input for complex move sequences

**Animation System:**
- Smooth 3D rotations using WPF's Storyboard animation
- Configurable animation speed
- Sequential animation of multiple moves
- Real-time visual feedback

**Settings & Customization:**
- Lighting control (ambient, key, fill, rim)
- Gloss and shininess adjustment
- Animation speed scaling
- Gap adjustment between cube pieces
- Rim lighting toggle for enhanced 3D perception

## 📊 Solving Algorithm Details

### Layer-by-Layer Method (Beginner's Method)

The solver follows the most popular beginner's method for teaching cube solving:

**Phase 1-2: First Layer (White Side)**
- Solves the white cross by matching edge colors with center pieces
- Completes corners by rotating them into position

**Phase 3: Second Layer (Middle)**
- Inserts four middle-layer edges while preserving the first layer
- Uses intuitive rotations and push-in techniques

**Phase 4-5: Third Layer Setup (Yellow Side)**
- Creates the yellow cross using a simple pattern
- Orients corners so yellow faces upward

**Phase 6-7: Final Layer**
- Positions remaining corners and edges
- Uses cycle algorithms to move pieces without disturbing solved layers
- Includes move optimization to remove redundant sequences

### Performance
- Typical solve time: < 150 moves (unoptimized)
- After optimization: 50-100 moves
- Near-optimal scrambles: often achieved in 60-80 moves

## 🎮 How to Use

### Running the Application
1. Open `Rubik's Cube.sln` in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Run the WPF application (F5)

### Playing with the Cube

**Rotate with Mouse:**
- Click and drag on the cube to rotate it in 3D space
- Watch the cube spin smoothly

**Enter Moves via Keyboard:**
- Type move sequences in the text box
- Examples: "R", "U'", "F2", "R U R' U'"
- Press Enter to execute

**Use Buttons:**
- Click "Scramble" to randomize the cube
- Click "Solve" to automatically solve it
- Click "Reset" to restore the solved state
- Click "Copy Solution" to copy the solution sequence

**Adjust Perspective:**
- Choose different face perspectives from the dropdown
- The keyboard mapping adjusts automatically

**Customize Visuals:**
- Adjust sliders for lighting, animation speed, and shininess
- Toggle rim lighting on/off for different visual styles
- Modify cube piece gaps

### Keyboard Shortcuts
```
R  - Rotate Right face 90° CW
R' - Rotate Right face 90° CCW
R2 - Rotate Right face 180°
(Same pattern for L, U, D, F, B, M)

Arrow Keys - Rotate entire cube
Ctrl+Z    - Undo last move
```

## 🔧 Building & Compilation

### Requirements
- Visual Studio 2022 or newer
- .NET 8 SDK
- C# 11 compiler support

### Build Instructions
```bash
# Restore NuGet packages
dotnet restore "Rubik's Cube.sln"

# Build the solution
dotnet build "Rubik's Cube.sln" -c Release

# Run the application
dotnet run --project "Rubik's Cube/Rubik's Cube.csproj"
```

## 📁 File Guide

| File | Purpose |
|------|---------|
| `RubiksCube.cs` | 540+ lines - Cube state model and move implementation |
| `BeginnersMethodSolver.cs` | 400+ lines - 7-phase solving algorithm |
| `MainWindow.xaml.cs` | 600+ lines - UI, 3D rendering, animation, input handling |
| `MainWindow.xaml` | XAML UI definition with layout and controls |
| `Builder/Generator.cs` | Geometry builder for cube pieces |
| `BevelGen/Generator.cs` | Bevel effect generator for realistic edges |

## 🎓 Learning Resources

This project demonstrates:
- **3D Graphics Programming** - WPF Media3D, transforms, animations
- **Algorithm Implementation** - Complex move simulation and solving logic
- **UI/UX Design** - Responsive controls and real-time feedback
- **Object-Oriented Design** - Separation of concerns (model, view, solver)
- **C# Best Practices** - Proper naming conventions, documentation, structure
- **Event-Driven Programming** - Keyboard/mouse input handling
- **Animation & Timing** - Smooth transitions and sequencing

## 💡 Future Enhancement Ideas

- [ ] Add speed cubing timer with splits
- [ ] Implement advanced solving methods (CFOP/Roux)
- [ ] Add multiplayer cube competition modes
- [ ] Export/import cube states
- [ ] Cube notation tutorial and guide
- [ ] Move history with playback
- [ ] Statistics tracking (average solve time, PB tracking)
- [ ] Support for 4×4, 5×5 cubes
- [ ] Mobile/touch support
- [ ] VR mode for immersive solving

## 📝 License

This project is provided as-is for educational purposes. Feel free to fork, modify, and extend it for your own projects.

## 👨‍💻 Author

Created as an educational project to demonstrate:
- 3D graphics programming with WPF
- Complex algorithmic problem-solving
- C# software engineering practices

## 🐛 Troubleshooting

**Cube not rendering?**
- Ensure graphics drivers are up to date
- Check that DirectX 11+ is installed
- Try adjusting lighting settings

**Animations too slow?**
- Increase "Animation Speed" slider
- Check system performance

**Solver taking too long?**
- This is normal for highly scrambled cubes
- The solver explores many possibilities

**Moves not responding?**
- Ensure the move input box has focus (click it first)
- Check that the move notation is valid
- Verify perspective-aware keyboard mapping is correct
- The screen updates to match the model.

## How to run it

Open [Rubik's Cube/Rubik's Cube/Rubik's Cube.csproj](Rubik's%20Cube/Rubik's%20Cube/Rubik's%20Cube.csproj) in Visual Studio or Visual Studio Code with the .NET SDK installed, then run the WPF project.

## Controls

- Use the move input or move buttons to turn the cube.
- Drag the mouse to rotate the whole view.
- Use scramble to mix the cube.
- Use solve to run the solver.