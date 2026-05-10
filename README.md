# Rubik's Cube

This project is a 3D Rubik's Cube app made with C# and WPF. It shows a cube on the screen, lets you rotate it, scramble it, and solve it with a beginner-style solving method. 
 
## What was used to make it 

The project was built with:

- C#
- .NET 8 
- WPF for the window and user interface
- WPF 3D for the cube model and animation
- A custom beginner-method solver for the solution logic

## How the code works

The app has two main parts:

- The cube model stores the real state of the Rubik's Cube.
- The window draws the cube in 3D and updates the picture after every move.

When the app starts, it builds the cube from 26 visible small cubes. The center piece is hidden because a real Rubik's Cube does not have a visible middle cubie. Each small cube has a dark body and colored sticker faces.

The main files do the following:

- [Rubik's Cube/Rubik's Cube/RubiksCube.cs](Rubik's%20Cube/Rubik's%20Cube/RubiksCube.cs) stores the cube faces and performs moves like R, L, U, D, F, B, and M.
- [Rubik's Cube/Rubik's Cube/MainWindow.xaml.cs](Rubik's%20Cube/Rubik's%20Cube/MainWindow.xaml.cs) builds the 3D scene, handles mouse control, animations, buttons, and settings.
- [Rubik's Cube/Rubik's Cube/BeginnersMethodSolver.cs](Rubik's%20Cube/Rubik's%20Cube/BeginnersMethodSolver.cs) solves the cube step by step.

## How solving works

The solver follows a simple beginner method:

1. White cross
2. White corners
3. Middle layer
4. Yellow cross
5. Yellow face
6. Top corners
7. Top edges

The solver looks at the current cube state, picks the next step, and adds the needed moves to a solution list.

## Simple view of the app

Think of it like this:

- The model knows what the cube really looks like.
- The window shows that cube in 3D.
- A move changes the model.
- The screen updates to match the model.

## How to run it

Open [Rubik's Cube/Rubik's Cube/Rubik's Cube.csproj](Rubik's%20Cube/Rubik's%20Cube/Rubik's%20Cube.csproj) in Visual Studio or Visual Studio Code with the .NET SDK installed, then run the WPF project.

## Controls

- Use the move input or move buttons to turn the cube.
- Drag the mouse to rotate the whole view.
- Use scramble to mix the cube.
- Use solve to run the solver.
