using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuGame
{
    /// <summary>Top scores from the backend, filterable by difficulty.</summary>
    public class LeaderboardScreen : MonoBehaviour
    {
        public Action Back;

        const int Rows = 10;
        static readonly string[] TabNames = { "All", "Easy", "Medium", "Hard" };

        class Row { public Text rank, name, score; }

        readonly Row[] rows = new Row[Rows];
        readonly Image[] tabImages = new Image[TabNames.Length];
        Text status, myRank;
        Difficulty? filter;
        string currentUser = "";
        int requestId;

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
            UIKit.Place(panel, UIKit.Center, new Vector2(0, 20), new Vector2(1100, 860));
            var content = UIKit.Content(panel);

            var title = UIKit.Label(content, "Hall of Heroes", 60, UIKit.TextLight);
            UIKit.Place(title.rectTransform, UIKit.TopCenter, new Vector2(0, -15), new Vector2(900, 80));

            for (int i = 0; i < TabNames.Length; i++)
            {
                int tab = i;
                var b = UIKit.MakeButton(content, TabNames[i], new Vector2(200, 56), UIKit.Slate, () => SelectTab(tab), 28);
                UIKit.Place((RectTransform)b.transform, UIKit.TopLeft, new Vector2(100 + i * 220, -100), new Vector2(200, 56));
                tabImages[i] = UIKit.Content((RectTransform)b.transform).GetComponent<Image>();
            }

            Header(content, "#", 80, 120, TextAnchor.MiddleCenter);
            Header(content, "Player", 250, 500, TextAnchor.MiddleLeft);
            Header(content, "Score", 780, 240, TextAnchor.MiddleCenter);

            for (int i = 0; i < Rows; i++)
            {
                float y = -210 - i * 56;
                if (i % 2 == 0)
                {
                    var stripe = UIKit.Box(content, "Stripe", new Color(1, 1, 1, 0.05f), UIKit.Rounded(10));
                    UIKit.Place(stripe.rectTransform, UIKit.TopCenter, new Vector2(0, y + 2), new Vector2(1040, 52));
                }
                rows[i] = new Row
                {
                    rank = Cell(content, 80, y, 120, TextAnchor.MiddleCenter),
                    name = Cell(content, 250, y, 500, TextAnchor.MiddleLeft),
                    score = Cell(content, 780, y, 240, TextAnchor.MiddleCenter)
                };
            }

            status = UIKit.Label(content, "", 30, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(status.rectTransform, UIKit.TopCenter, new Vector2(0, -440), new Vector2(900, 60));

            myRank = UIKit.Label(content, "", 30, UIKit.Accent, TextAnchor.MiddleCenter, false);
            UIKit.Place(myRank.rectTransform, UIKit.TopCenter, new Vector2(0, -785), new Vector2(1000, 50));

            var back = UIKit.MakeButton(root, "Back", new Vector2(300, 80), UIKit.Blue, () => Back?.Invoke(), 36);
            UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(300, 80));
        }

        static void Header(RectTransform parent, string text, float x, float width, TextAnchor anchor)
        {
            var t = UIKit.Label(parent, text, 30, UIKit.Muted, anchor, false);
            UIKit.Place(t.rectTransform, UIKit.TopLeft, new Vector2(x, -165), new Vector2(width, 40));
        }

        static Text Cell(RectTransform parent, float x, float y, float width, TextAnchor anchor)
        {
            var t = UIKit.Label(parent, "", 34, UIKit.TextLight, anchor, false);
            UIKit.Place(t.rectTransform, UIKit.TopLeft, new Vector2(x, y), new Vector2(width, 52));
            return t;
        }

        void OnDisable()
        {
            requestId++;
        }

        public void Show(string username)
        {
            currentUser = username;
            filter = null;
            Refresh();
        }

        void SelectTab(int index)
        {
            filter = index == 0 ? (Difficulty?)null : (Difficulty)(index - 1);
            Refresh();
        }

        void Refresh()
        {
            int id = ++requestId;

            for (int i = 0; i < tabImages.Length; i++)
            {
                bool selected = i == (filter.HasValue ? (int)filter.Value + 1 : 0);
                tabImages[i].color = selected ? UIKit.Blue : UIKit.Slate;
            }

            foreach (var r in rows) r.rank.text = r.name.text = r.score.text = "";
            status.text = "Loading...";
            myRank.text = "";

            var api = ApiClient.Instance;
            api.GetLeaderboard(filter, Rows, result =>
            {
                if (id != requestId) return;
                if (!result.Ok) { status.text = result.error; return; }
                status.text = result.data.Length == 0 ? "No scores yet. Be the first!" : "";
                for (int i = 0; i < Rows && i < result.data.Length; i++)
                {
                    var e = result.data[i];
                    var color = string.Equals(e.username, currentUser, StringComparison.OrdinalIgnoreCase) ? UIKit.Accent : UIKit.TextLight;
                    rows[i].rank.text = e.rank.ToString();
                    rows[i].name.text = e.username;
                    rows[i].score.text = e.score.ToString();
                    rows[i].rank.color = rows[i].name.color = rows[i].score.color = color;
                }
            });

            api.GetMyRank(filter, result =>
            {
                if (id != requestId) return;
                if (result.sessionExpired) myRank.text = "Log in again to see your rank.";
                else if (!result.Ok) myRank.text = "";
                else myRank.text = result.data == null ? "You have no rank here yet." : $"Your rank: #{result.data.rank}  ({result.data.score} pts)";
            });
        }
    }
}
