using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuGame
{
    public class MenuScreen : MonoBehaviour
    {
        public Action<Difficulty> StartGame;
        public Action ShowLeaderboard;
        public Action ChangeName;

        Text welcome, total;

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
            total = UIKit.Label(UIKit.Content(plaque), "", 28, UIKit.Accent, TextAnchor.MiddleLeft, false);
            UIKit.Place(total.rectTransform, UIKit.TopLeft, new Vector2(25, -62), new Vector2(400, 40));

            AddDifficulty(root, Difficulty.Easy, UIKit.Green, 80);
            AddDifficulty(root, Difficulty.Medium, UIKit.Blue, -50);
            AddDifficulty(root, Difficulty.Hard, UIKit.Red, -180);

            AddSmallButton(root, "Leaderboard", UIKit.Purple, -340, () => ShowLeaderboard?.Invoke());
            AddSmallButton(root, "Change Name", UIKit.Orange, 0, () => ChangeName?.Invoke());
            AddSmallButton(root, "Quit", UIKit.Slate, 340, Quit);

            var rules = UIKit.Label(root,
                "Solve faster for more points. Every mistake costs points. Auto-Solve forfeits all points.",
                28, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(rules.rectTransform, UIKit.Center, new Vector2(0, -450), new Vector2(1500, 50));
        }

        void AddDifficulty(RectTransform root, Difficulty d, Color color, float y)
        {
            var b = UIKit.MakeButton(root, $"{d}  -  up to {ScoreRules.StartingPoints(d)} pts", new Vector2(640, 110), color,
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

        public void Show(string nickname)
        {
            welcome.text = nickname;
            total.text = $"Total points: {SaveData.Current.TotalPointsFor(nickname)}";
        }
    }
}
