using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuGame
{
    public class ResultScreen : MonoBehaviour
    {
        public Action PlayAgain;
        public Action ToMenu;

        Text title, difficultyValue, timeValue, mistakesValue;
        Text startValue, timePenaltyValue, mistakePenaltyValue, totalValue, rankText, forfeitText;
        GameObject breakdown;

        public static ResultScreen Create(Transform parent)
        {
            var rt = UIKit.NewRect(parent, "ResultScreen");
            var screen = rt.gameObject.AddComponent<ResultScreen>();
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
            UIKit.Place(panel, UIKit.Center, Vector2.zero, new Vector2(900, 920));
            var content = UIKit.Content(panel);

            title = UIKit.Label(content, "", 80, UIKit.Accent);
            UIKit.Place(title.rectTransform, UIKit.TopCenter, new Vector2(0, -30), new Vector2(800, 110));

            difficultyValue = Row(content, "Difficulty", -170, 36, UIKit.TextLight);
            timeValue = Row(content, "Time", -225, 36, UIKit.TextLight);
            mistakesValue = Row(content, "Mistakes", -280, 36, UIKit.TextLight);

            var divider = UIKit.Box(content, "Divider", new Color(1, 1, 1, 0.15f));
            UIKit.Place(divider.rectTransform, UIKit.TopCenter, new Vector2(0, -350), new Vector2(700, 3));

            var group = UIKit.NewRect(content, "Breakdown");
            UIKit.Stretch(group);
            breakdown = group.gameObject;
            startValue = Row(group, "Starting points", -375, 34, UIKit.TextLight);
            timePenaltyValue = Row(group, "Time penalty", -430, 34, new Color(1f, 0.6f, 0.5f));
            mistakePenaltyValue = Row(group, "Mistake penalty", -485, 34, new Color(1f, 0.6f, 0.5f));
            totalValue = Row(group, "TOTAL", -560, 60, UIKit.Accent);

            forfeitText = UIKit.Label(content, "Auto-Solve was used.\nAll points are forfeited.", 40,
                new Color(1f, 0.6f, 0.5f));
            UIKit.Place(forfeitText.rectTransform, UIKit.TopCenter, new Vector2(0, -420), new Vector2(760, 150));

            rankText = UIKit.Label(content, "", 34, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(rankText.rectTransform, UIKit.TopCenter, new Vector2(0, -660), new Vector2(760, 50));

            var again = UIKit.MakeButton(content, "Play Again", new Vector2(340, 90), UIKit.Green, () => PlayAgain?.Invoke(), 38);
            UIKit.Place((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(-190, 50), new Vector2(340, 90));
            var menu = UIKit.MakeButton(content, "Main Menu", new Vector2(340, 90), UIKit.Blue, () => ToMenu?.Invoke(), 38);
            UIKit.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(190, 50), new Vector2(340, 90));
        }

        // Returns the right-hand value label; the left-hand caption is created here too.
        static Text Row(RectTransform parent, string caption, float y, int size, Color color)
        {
            var left = UIKit.Label(parent, caption, size, color, TextAnchor.MiddleLeft, false);
            UIKit.Place(left.rectTransform, UIKit.TopLeft, new Vector2(90, y), new Vector2(440, size + 20));
            var right = UIKit.Label(parent, "", size, color, TextAnchor.MiddleRight, false);
            UIKit.Place(right.rectTransform, new Vector2(1, 1), new Vector2(-90, y), new Vector2(300, size + 20));
            return right;
        }

        /// <param name="rank">1-based leaderboard position, or 0 when the game was not recorded.</param>
        public void Show(GameResult r, int rank)
        {
            var d = r.difficulty;
            difficultyValue.text = d.ToString();
            timeValue.text = ScoreRules.FormatTime(r.seconds);
            mistakesValue.text = r.mistakes.ToString();

            breakdown.SetActive(!r.forfeited);
            forfeitText.gameObject.SetActive(r.forfeited);
            title.text = r.forfeited ? "Puzzle Solved" : "VICTORY!";
            title.color = r.forfeited ? UIKit.TextLight : UIKit.Accent;

            if (r.forfeited)
            {
                rankText.text = "This game does not count for the leaderboard.";
                return;
            }

            startValue.text = "+" + ScoreRules.StartingPoints(d);
            timePenaltyValue.text = "-" + ScoreRules.TimePenalty(d, r.seconds);
            mistakePenaltyValue.text = "-" + ScoreRules.MistakePenalty(d, r.mistakes);
            totalValue.text = r.score.ToString();
            rankText.text = rank > 0 ? $"Leaderboard position: #{rank}" : "";
        }
    }
}
