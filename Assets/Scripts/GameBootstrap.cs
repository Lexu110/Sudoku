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
        AuthScreen authScreen;
        MenuScreen menuScreen;
        GameScreen gameScreen;
        ResultScreen resultScreen;
        LeaderboardScreen leaderboardScreen;
        SettingsScreen settingsScreen;
        ApiClient api;
        readonly List<GameObject> screens = new List<GameObject>();

        Difficulty lastDifficulty = Difficulty.Easy;
        // A finished game whose score has not been saved yet.
        GameResult pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("Sudoku Game").AddComponent<GameBootstrap>();
        }

        void Start()
        {
            Application.targetFrameRate = 60;
            api = gameObject.AddComponent<ApiClient>();
            var canvas = BuildCanvas();
            EnsureEventSystem();

            authScreen = AuthScreen.Create(canvas);
            menuScreen = MenuScreen.Create(canvas);
            gameScreen = GameScreen.Create(canvas);
            resultScreen = ResultScreen.Create(canvas);
            leaderboardScreen = LeaderboardScreen.Create(canvas);
            settingsScreen = SettingsScreen.Create(canvas);
            screens.AddRange(new[]
            {
                authScreen.gameObject, menuScreen.gameObject, gameScreen.gameObject,
                resultScreen.gameObject, leaderboardScreen.gameObject, settingsScreen.gameObject
            });

            authScreen.Submitted = OnAuthSubmitted;

            menuScreen.StartGame = StartGame;
            menuScreen.ShowLeaderboard = ShowLeaderboard;
            menuScreen.OpenSettings = () => Show(settingsScreen.gameObject);
            menuScreen.LogOut = () =>
            {
                SaveData.Current.ClearSession();
                ShowAuth(null);
            };

            gameScreen.Finished = OnGameFinished;
            gameScreen.ExitRequested = ShowMenu;

            resultScreen.PlayAgain = () => { pending = null; StartGame(lastDifficulty); };
            resultScreen.ToMenu = () => { pending = null; ShowMenu(); };
            resultScreen.Retry = () => { if (pending != null) SubmitScore(pending); };
            leaderboardScreen.Back = ShowMenu;
            settingsScreen.Back = ShowMenu;

            StartSession();
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

        // ---------- Login ----------

        void StartSession()
        {
            if (!SaveData.Current.HasSession)
            {
                ShowAuth(null);
                return;
            }

            Show(authScreen.gameObject);
            authScreen.SetBusy("Signing you in...");
            api.CheckSession(state =>
            {
                if (state == SessionState.Expired) ShowAuth("Your session expired. Please log in again.");
                else ShowMenu();
            });
        }

        void ShowAuth(string message)
        {
            Show(authScreen.gameObject);
            authScreen.Open(message);
        }

        void OnAuthSubmitted(string username, string password, bool register)
        {
            authScreen.SetBusy(register ? "Creating your account..." : "Logging in...");
            System.Action<string> done = error =>
            {
                if (error != null) authScreen.ShowError(error);
                else AfterLogin();
            };
            if (register) api.Register(username, password, done);
            else api.Login(username, password, done);
        }

        void AfterLogin()
        {
            if (pending == null)
            {
                ShowMenu();
                return;
            }
            Show(resultScreen.gameObject);
            resultScreen.Show(pending);
            SubmitScore(pending);
        }

        // ---------- Screens ----------

        void ShowMenu()
        {
            Show(menuScreen.gameObject);
            menuScreen.Show(SaveData.Current.username);
            api.GetMyRank(null, result =>
            {
                if (!menuScreen.gameObject.activeSelf) return;
                if (result.sessionExpired) ShowAuth("Your session expired. Please log in again.");
                else if (result.Ok)
                    menuScreen.SetRank(result.data == null ? "No rank yet" : $"Rank #{result.data.rank}  -  {result.data.score} pts");
            });
        }

        void ShowLeaderboard()
        {
            Show(leaderboardScreen.gameObject);
            leaderboardScreen.Show(SaveData.Current.username);
        }

        void StartGame(Difficulty d)
        {
            lastDifficulty = d;
            Show(gameScreen.gameObject);
            gameScreen.Begin(d, SaveData.Current.username);
        }

        void OnGameFinished(GameResult result)
        {
            Show(resultScreen.gameObject);
            resultScreen.Show(result);
            if (result.forfeited) return;

            pending = result;
            SubmitScore(result);
        }

        void SubmitScore(GameResult result)
        {
            resultScreen.ShowSubmitting();
            api.SubmitScore(result.difficulty, result.seconds, result.mistakes, response =>
            {
                // The player already left the result screen.
                if (pending != result) return;

                if (response.sessionExpired)
                {
                    ShowAuth("Your session expired. Log in to save your score.");
                    return;
                }
                if (!response.Ok)
                {
                    resultScreen.ShowError(response.error);
                    return;
                }

                pending = null;
                resultScreen.ShowScore(response.data);
                api.GetMyRank(result.difficulty, rank =>
                {
                    if (rank.Ok) resultScreen.ShowRank(rank.data, result.difficulty);
                });
            });
        }
    }
}
