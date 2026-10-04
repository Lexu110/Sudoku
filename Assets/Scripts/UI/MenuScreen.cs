using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuGame
{
    public class MenuScreen : MonoBehaviour
    {
        public Action<Difficulty> StartGame;
        public Action ShowLeaderboard;
        public Action OpenSettings;
        public Action LogOut;

        Text welcome, rank, padHint;

        public static MenuScreen Create(Transform parent)
        {
            var rt = UIKit.NewRect(parent, "MenuScreen");
            var screen = rt.gameObject.AddComponent<MenuScreen>();
            screen.Build();
            return screen;
        }

        void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            gameObject.AddComponent<CanvasGroup>();
            gameObject.AddComponent<PopIn>();
            UIKit.ScreenBackground(root);

            var halo = UIKit.Box(root, "TitleGlow", new Color(0.4f, 0.3f, 1f, 0.45f), UIKit.Glow);
            UIKit.Place(halo.rectTransform, UIKit.Center, new Vector2(0, 340), new Vector2(1000, 280));
            var title = UIKit.Label(root, "SUDOKU", 170, UIKit.TextLight);
            UIKit.Place(title.rectTransform, UIKit.Center, new Vector2(0, 340), new Vector2(1200, 220));
            var sub = UIKit.Label(root, "Choose your challenge", 40, UIKit.Accent);
            UIKit.Place(sub.rectTransform, UIKit.Center, new Vector2(0, 215), new Vector2(900, 60));

            var plaque = UIKit.FramedPanel(root, "PlayerPlaque", UIKit.SurfaceLight, 5, 18);
            UIKit.Place(plaque, UIKit.TopLeft, new Vector2(40, -40), new Vector2(460, 120));
            welcome = UIKit.Label(UIKit.Content(plaque), "", 36, UIKit.TextLight, TextAnchor.MiddleLeft);
            UIKit.Place(welcome.rectTransform, UIKit.TopLeft, new Vector2(25, -12), new Vector2(400, 50));
            rank = UIKit.Label(UIKit.Content(plaque), "", 28, UIKit.Accent, TextAnchor.MiddleLeft, false);
            UIKit.Place(rank.rectTransform, UIKit.TopLeft, new Vector2(25, -62), new Vector2(420, 40));

            AddDifficulty(root, Difficulty.Easy, UIKit.Green, 80);
            AddDifficulty(root, Difficulty.Medium, UIKit.Blue, -50);
            AddDifficulty(root, Difficulty.Hard, UIKit.Red, -180);

            AddSmallButton(root, "Leaderboard", UIKit.Purple, -480, () => ShowLeaderboard?.Invoke());
            AddSmallButton(root, "Settings", UIKit.Blue, -160, () => OpenSettings?.Invoke());
            AddSmallButton(root, "Log Out", UIKit.Orange, 160, () => LogOut?.Invoke());
            AddSmallButton(root, "Quit", UIKit.Slate, 480, Quit);

            var rules = UIKit.Label(root,
                "Solve faster for more points. Every mistake costs points. Auto-Solve forfeits all points.",
                28, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(rules.rectTransform, UIKit.Center, new Vector2(0, -450), new Vector2(1500, 50));

            padHint = UIKit.Label(root,
                "Controller detected:  D-pad / Stick  Navigate     A  Select",
                28, UIKit.Accent, TextAnchor.MiddleCenter, false);
            UIKit.Place(padHint.rectTransform, UIKit.Center, new Vector2(0, -500), new Vector2(1500, 44));
        }

        void Update()
        {
            padHint.gameObject.SetActive(UnityEngine.InputSystem.Gamepad.current != null);
        }

        void AddDifficulty(RectTransform root, Difficulty d, Color color, float y)
        {
            var b = UIKit.MakeButton(root, d.ToString(), new Vector2(640, 110), color,
                () => StartGame?.Invoke(d), 38);
            UIKit.Place((RectTransform)b.transform, UIKit.Center, new Vector2(0, y), new Vector2(640, 110));
        }

        void AddSmallButton(RectTransform root, string label, Color color, float x, Action onClick)
        {
            var b = UIKit.MakeButton(root, label, new Vector2(300, 80), color, onClick, 32);
            UIKit.Place((RectTransform)b.transform, UIKit.Center, new Vector2(x, -330), new Vector2(300, 80));
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Show(string username)
        {
            welcome.text = username;
            rank.text = "";
        }

        public void SetRank(string text)
        {
            rank.text = text;
        }
    }
}
