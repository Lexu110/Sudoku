using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuGame
{
    public class LeaderboardScreen : MonoBehaviour
    {
        public Action Back;

        const int Rows = 10;

        class Row { public Text rank, name, score, mode, time; }

        readonly Row[] rows = new Row[Rows];

        public static LeaderboardScreen Create(Transform parent)
        {
            var rt = UIKit.NewRect(parent, "LeaderboardScreen");
            var screen = rt.gameObject.AddComponent<LeaderboardScreen>();
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

            var panel = UIKit.FramedPanel(root, "Panel", UIKit.SurfaceLight);
            UIKit.Place(panel, UIKit.Center, new Vector2(0, 35), new Vector2(1100, 840));
            var content = UIKit.Content(panel);

            var title = UIKit.Label(content, "Hall of Heroes", 64, UIKit.TextLight);
            UIKit.Place(title.rectTransform, UIKit.TopCenter, new Vector2(0, -20), new Vector2(900, 90));

            Header(content, "#", 30, 80, TextAnchor.MiddleCenter);
            Header(content, "Player", 120, 380, TextAnchor.MiddleLeft);
            Header(content, "Score", 510, 180, TextAnchor.MiddleCenter);
            Header(content, "Mode", 700, 180, TextAnchor.MiddleCenter);
            Header(content, "Time", 890, 160, TextAnchor.MiddleCenter);

            for (int i = 0; i < Rows; i++)
            {
                float y = -150 - i * 60;
                if (i % 2 == 0)
                {
                    var stripe = UIKit.Box(content, "Stripe", new Color(1, 1, 1, 0.05f), UIKit.Rounded(10));
                    UIKit.Place(stripe.rectTransform, UIKit.TopCenter, new Vector2(0, y + 2), new Vector2(1040, 56));
                }
                rows[i] = new Row
                {
                    rank = Cell(content, 30, y, 80, TextAnchor.MiddleCenter),
                    name = Cell(content, 120, y, 380, TextAnchor.MiddleLeft),
                    score = Cell(content, 510, y, 180, TextAnchor.MiddleCenter),
                    mode = Cell(content, 700, y, 180, TextAnchor.MiddleCenter),
                    time = Cell(content, 890, y, 160, TextAnchor.MiddleCenter)
                };
            }

            var back = UIKit.MakeButton(root, "Back", new Vector2(300, 80), UIKit.Blue, () => Back?.Invoke(), 36);
            UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(300, 80));
        }

        static void Header(RectTransform parent, string text, float x, float width, TextAnchor anchor)
        {
            var t = UIKit.Label(parent, text, 30, UIKit.Muted, anchor, false);
            UIKit.Place(t.rectTransform, UIKit.TopLeft, new Vector2(x, -105), new Vector2(width, 40));
        }

        static Text Cell(RectTransform parent, float x, float y, float width, TextAnchor anchor)
        {
            var t = UIKit.Label(parent, "", 34, UIKit.TextLight, anchor, false);
            UIKit.Place(t.rectTransform, UIKit.TopLeft, new Vector2(x, y), new Vector2(width, 52));
            return t;
        }

        public void Show(string currentPlayer)
        {
            var scores = SaveData.Current.scores;
            for (int i = 0; i < Rows; i++)
            {
                var r = rows[i];
                if (i >= scores.Count)
                {
                    r.rank.text = r.name.text = r.score.text = r.mode.text = r.time.text = "";
                    continue;
                }
                var s = scores[i];
                Color c = s.nickname == currentPlayer ? UIKit.Accent : UIKit.TextLight;
                r.rank.text = (i + 1).ToString();
                r.name.text = s.nickname;
                r.score.text = s.score.ToString();
                r.mode.text = ((Difficulty)s.difficulty).ToString();
                r.time.text = ScoreRules.FormatTime(s.seconds);
                r.rank.color = r.name.color = r.score.color = r.mode.color = r.time.color = c;
            }
        }
    }
}
