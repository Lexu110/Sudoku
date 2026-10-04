using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SudokuGame
{
    /// <summary>
    /// Entry point. It creates itself when the scene starts, so no scene setup is needed.
    /// Builds the canvas and all screens, and moves between them.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        NicknameScreen nicknameScreen;
        MenuScreen menuScreen;
        GameScreen gameScreen;
        ResultScreen resultScreen;
        LeaderboardScreen leaderboardScreen;
        SettingsScreen settingsScreen;
        readonly List<GameObject> screens = new List<GameObject>();

        Difficulty lastDifficulty = Difficulty.Easy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("Sudoku Game").AddComponent<GameBootstrap>();
        }

        void Start()
        {
            Application.targetFrameRate = 60;
            var canvas = BuildCanvas();
            EnsureEventSystem();

            nicknameScreen = NicknameScreen.Create(canvas);
            menuScreen = MenuScreen.Create(canvas);
            gameScreen = GameScreen.Create(canvas);
            resultScreen = ResultScreen.Create(canvas);
            leaderboardScreen = LeaderboardScreen.Create(canvas);
            settingsScreen = SettingsScreen.Create(canvas);
            screens.AddRange(new[]
            {
                nicknameScreen.gameObject, menuScreen.gameObject, gameScreen.gameObject,
                resultScreen.gameObject, leaderboardScreen.gameObject, settingsScreen.gameObject
            });

            nicknameScreen.Confirmed = name =>
            {
                SaveData.Current.nickname = name;
                SaveData.Current.Save();
                ShowMenu();
            };
            nicknameScreen.Cancelled = ShowMenu;

            menuScreen.StartGame = StartGame;
            menuScreen.ShowLeaderboard = ShowLeaderboard;
            menuScreen.OpenSettings = () => Show(settingsScreen.gameObject);
            menuScreen.ChangeName = () =>
            {
                Show(nicknameScreen.gameObject);
                nicknameScreen.Show(SaveData.Current.nickname, true);
            };

            gameScreen.Finished = OnGameFinished;
            gameScreen.ExitRequested = ShowMenu;

            resultScreen.PlayAgain = () => StartGame(lastDifficulty);
            resultScreen.ToMenu = ShowMenu;
            leaderboardScreen.Back = ShowMenu;
            settingsScreen.Back = ShowMenu;

            if (string.IsNullOrEmpty(SaveData.Current.nickname))
            {
                Show(nicknameScreen.gameObject);
                nicknameScreen.Show("", false);
            }
            else ShowMenu();
        }

        Transform BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        void Show(GameObject target)
        {
            foreach (var s in screens) s.SetActive(s == target);
        }

        void ShowMenu()
        {
            Show(menuScreen.gameObject);
            menuScreen.Show(SaveData.Current.nickname);
        }

        void ShowLeaderboard()
        {
            Show(leaderboardScreen.gameObject);
            leaderboardScreen.Show(SaveData.Current.nickname);
        }

        void StartGame(Difficulty d)
        {
            lastDifficulty = d;
            Show(gameScreen.gameObject);
            gameScreen.Begin(d, SaveData.Current.nickname);
        }

        void OnGameFinished(GameResult result)
        {
            int rank = 0;
            if (!result.forfeited)
            {
                var entry = new ScoreEntry
                {
                    nickname = SaveData.Current.nickname,
                    score = result.score,
                    difficulty = (int)result.difficulty,
                    seconds = result.seconds,
                    mistakes = result.mistakes
                };
                SaveData.Current.AddScore(entry);
                rank = SaveData.Current.scores.IndexOf(entry) + 1;
            }
            Show(resultScreen.gameObject);
            resultScreen.Show(result, rank);
        }
    }
}
