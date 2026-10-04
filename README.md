# Sudoku

A modern, neon-styled Sudoku game built with Unity. Create an account, choose a difficulty, and race the clock for points on an online leaderboard.

## Features

- Account registration and login, with a global online leaderboard ("Hall of Heroes") filterable by difficulty
- Three difficulties (Easy, Medium, Hard) with generated puzzles that always have a single solution
- Score based on solve time, with points deducted for every mistake
- Auto-Solve available, but it forfeits all points and is not submitted
- Mouse and keyboard controls
- Runs on macOS, Windows and Linux

## Scoring

The backend calculates the score from the difficulty, solve time and number of mistakes, so the game only reports those values and shows the score the server returns. Faster solves score higher and every mistake costs points. Solves faster than 20 seconds are reported as 20 seconds, the server minimum.

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
- Internet access to the Sudoku backend (`https://sudoku.lcorream.com/api/v1`, set in `ApiClient.BaseUrl`)

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
    ApiClient.cs          Backend calls: login, register, scores, leaderboard
    SaveData.cs           Logged-in username and token (PlayerPrefs JSON)
    TimeFormat.cs         mm:ss formatting
    KeyBindings.cs        Rebindable keys (PlayerPrefs JSON)
  UI/
    UIKit.cs              Procedural sprites, theme colours and widget builders
    AuthScreen.cs, MenuScreen.cs, GameScreen.cs,
    ResultScreen.cs, LeaderboardScreen.cs, SettingsScreen.cs
```

## Save data

The username and login token (JWT, valid about 30 days) are stored locally with `PlayerPrefs` under the key `sudoku_save_v2`. The password is never stored. Key bindings are stored under `sudoku_keys_v1`.

