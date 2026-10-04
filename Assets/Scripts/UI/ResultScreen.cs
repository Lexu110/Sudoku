using System;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuGame
{
    /// <summary>Shows the finished game. The score comes from the backend, so it appears after the submit call returns.</summary>
    public class ResultScreen : MonoBehaviour
    {
        public Action PlayAgain;
        public Action ToMenu;
        public Action Retry;

        Text title, difficultyValue, timeValue, mistakesValue, scoreValue, statusText, rankText;
        GameObject retryButton;

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

            scoreValue = Row(content, "SCORE", -385, 60, UIKit.Accent);

            statusText = UIKit.Label(content, "", 32, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(statusText.rectTransform, UIKit.TopCenter, new Vector2(0, -490), new Vector2(760, 100));

            rankText = UIKit.Label(content, "", 34, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(rankText.rectTransform, UIKit.TopCenter, new Vector2(0, -600), new Vector2(760, 50));

            var retry = UIKit.MakeButton(content, "Retry", new Vector2(340, 80), UIKit.Orange, () => Retry?.Invoke(), 34);
            UIKit.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(340, 80));
            retryButton = retry.gameObject;

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

        public void Show(GameResult r)
        {
            difficultyValue.text = r.difficulty.ToString();
            timeValue.text = TimeFormat.Format(r.seconds);
            mistakesValue.text = r.mistakes.ToString();
            rankText.text = "";
            retryButton.SetActive(false);

            if (r.forfeited)
            {
                title.text = "Puzzle Solved";
                title.color = UIKit.TextLight;
                scoreValue.text = "0";
                SetStatus("Auto-Solve was used. All points are forfeited and this game is not saved to the leaderboard.", false);
                return;
            }

            title.text = "VICTORY!";
            title.color = UIKit.Accent;
            ShowSubmitting();
        }

        public void ShowSubmitting()
        {
            scoreValue.text = "...";
            rankText.text = "";
            retryButton.SetActive(false);
            SetStatus("Saving your score...", false);
        }

        public void ShowScore(ScoreOut score)
        {
            scoreValue.text = score.score.ToString();
            retryButton.SetActive(false);
            SetStatus("Score saved to the leaderboard.", false);
        }

        public void ShowRank(LeaderboardEntry entry, Difficulty difficulty)
        {
            rankText.text = entry == null ? "" : $"{difficulty} rank: #{entry.rank}";
        }

        public void ShowError(string message)
        {
            scoreValue.text = "-";
            retryButton.SetActive(true);
            SetStatus("Couldn't save your score. " + message, true);
        }

        void SetStatus(string message, bool isError)
        {
            statusText.text = message;
            statusText.color = isError ? new Color(1f, 0.55f, 0.5f) : UIKit.Muted;
        }
    }
}
