# Sudoku

A modern, neon-styled Sudoku game built with Unity. Pick a nickname, choose a difficulty, and race the clock for points.

## Features

- Nickname profile and a local leaderboard ("Hall of Heroes")
- Three difficulties (Easy, Medium, Hard) with generated puzzles that always have a single solution
- Score based on solve time, with points deducted for every mistake
- Auto-Solve available, but it forfeits all points and is not recorded on the leaderboard
- Mouse and keyboard controls
- Runs on macOS, Windows and Linux

## Scoring

`score = starting points - (seconds x points lost per second) - (mistakes x points lost per mistake)`, never below 0.

| Difficulty | Starting points | Lost per second | Lost per mistake |
|------------|-----------------|-----------------|------------------|
| Easy       | 1000            | 1               | 50               |
| Medium     | 2000            | 2               | 100              |
| Hard       | 3000            | 3               | 150              |

## Controls

| Action              | Input                        |
|---------------------|------------------------------|
| Select a cell       | Click, or arrow keys / WASD  |
| Enter a number      | On-screen pad, or keys 1-9   |
| Erase a wrong entry | Erase button, Backspace/Del  |
| Leave the game      | Leave Game button, or Esc    |

Movement, Erase and Leave Game each have a primary and an alternate key that you can change in **Settings** on the main menu. Number keys 1-9 are fixed.

A wrong number stays on the board in red and counts as a mistake until you erase it. Correct numbers are locked.

## Requirements

- Unity 6000.6.4f1 (Unity 6)
- Universal Render Pipeline (2D template), Input System package

## Getting started

1. Clone the repository and open the folder in Unity Hub with the editor version above.
2. Open `Assets/Scenes/SampleScene` and press Play. The game builds its UI from code at startup, so no extra scene setup is needed.

## Building

1. In Unity Hub, add the Windows and Linux build support modules (Mono) to your editor install if you need them.
2. Open **File > Build Profiles**, add the macOS, Windows and Linux standalone platforms, and build.

## Project structure

```
Assets/Scripts/
  GameBootstrap.cs        Entry point; creates the canvas and moves between screens
  Core/
    SudokuPuzzle.cs       Puzzle generator and solver
    ScoreRules.cs         Scoring rules and time formatting
    SaveData.cs           Nickname and leaderboard (PlayerPrefs JSON)
    KeyBindings.cs        Rebindable keys (PlayerPrefs JSON)
  UI/
    UIKit.cs              Procedural sprites, theme colours and widget builders
    NicknameScreen.cs, MenuScreen.cs, GameScreen.cs,
    ResultScreen.cs, LeaderboardScreen.cs, SettingsScreen.cs
```

## Save data

The nickname and leaderboard are stored locally with `PlayerPrefs` under the key `sudoku_save_v1`.

