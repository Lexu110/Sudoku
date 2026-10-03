using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SudokuGame
{
    public class GameResult
    {
        public Difficulty difficulty;
        public float seconds;
        public int mistakes;
        public int score;
        public bool forfeited;
    }

    /// <summary>One square of the board. Handles its own shake (wrong number) and pulse (victory wave).</summary>
    public class CellView : MonoBehaviour
    {
        public Image bg;
        public Text text;
        Vector2 basePos;
        float shake, pulse;

        const float ShakeTime = 0.45f, PulseTime = 0.4f;

        public void Init(Vector2 position) { basePos = position; }
        public void Shake() { shake = ShakeTime; }
        public void Pulse() { pulse = PulseTime; }

        void Update()
        {
            var rt = (RectTransform)transform;
            float x = 0f;
            if (shake > 0f)
            {
                shake -= Time.unscaledDeltaTime;
                x = Mathf.Sin(shake * 80f) * 9f * Mathf.Clamp01(shake / ShakeTime);
            }
            rt.anchoredPosition = basePos + new Vector2(x, 0f);

            float scale = 1f;
            if (pulse > 0f)
            {
                pulse -= Time.unscaledDeltaTime;
                scale = 1f + 0.18f * Mathf.Sin(Mathf.Clamp01(1f - pulse / PulseTime) * Mathf.PI);
            }
            rt.localScale = Vector3.one * scale;
        }
    }

    public class GameScreen : MonoBehaviour
    {
        public Action<GameResult> Finished;
        public Action ExitRequested;

        enum State { Idle, Playing, Won, Forfeited }

        const float CellSize = 80f, Gap = 4f, BoxExtra = 6f, Pad = 16f;

        static readonly Color GivenColor = new Color(0.21f, 0.25f, 0.42f);
        static readonly Color EmptyColor = new Color(0.13f, 0.16f, 0.30f);
        static readonly Color PeerTint = new Color(0.24f, 0.32f, 0.58f);
        static readonly Color SameTint = new Color(0.17f, 0.55f, 0.75f);
        static readonly Color SelectedColor = new Color(0.28f, 0.45f, 0.90f);

        readonly CellView[] cells = new CellView[81];
        readonly int[] values = new int[81];
        readonly bool[] isGiven = new bool[81];
        readonly bool[] autoFilled = new bool[81];
        readonly Button[] padButtons = new Button[10];
        readonly Text[] padCounts = new Text[10];

        SudokuPuzzle puzzle;
        Difficulty difficulty;
        State state = State.Idle;
        float elapsed;
        int mistakes;
        int selected = -1;

        Text headerText, timeText, mistakeText, scoreText;
        RectTransform selectionFrame;
        Vector2 frameTarget;
        GameObject dialog;

        public static GameScreen Create(Transform parent)
        {
            var rt = UIKit.NewRect(parent, "GameScreen");
            var screen = rt.gameObject.AddComponent<GameScreen>();
            screen.Build();
            return screen;
        }

        // ---------- UI construction ----------

        void Build()
        {
            var root = (RectTransform)transform;
            UIKit.Stretch(root);
            gameObject.AddComponent<CanvasGroup>();
            gameObject.AddComponent<PopIn>();
            UIKit.ScreenBackground(root);

            headerText = UIKit.Label(root, "", 54, UIKit.TextLight);
            UIKit.Place(headerText.rectTransform, UIKit.TopCenter, new Vector2(0, -30), new Vector2(1400, 90));

            BuildBoard(root);
            BuildSidePanel(root);
        }

        static Vector2 CellCenter(int row, int col)
        {
            float x = Pad + col * (CellSize + Gap) + (col / 3) * BoxExtra + CellSize * 0.5f;
            float y = Pad + row * (CellSize + Gap) + (row / 3) * BoxExtra + CellSize * 0.5f;
            return new Vector2(x, -y);
        }

        void BuildBoard(RectTransform root)
        {
            var board = UIKit.FramedPanel(root, "Board", UIKit.SurfaceLight, 8, 28);
            UIKit.Place(board, UIKit.Center, new Vector2(-295, -45), new Vector2(812, 812));
            var grid = UIKit.Content(board);

            for (int i = 0; i < 81; i++)
            {
                var img = UIKit.Box(grid, "Cell" + i, EmptyColor, UIKit.Rounded(14), true);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = UIKit.TopLeft;
                rt.pivot = UIKit.Center;
                rt.sizeDelta = new Vector2(CellSize, CellSize);

                var text = UIKit.Label(rt, "", 54, UIKit.TextLight, TextAnchor.MiddleCenter, false);
                UIKit.Stretch(text.rectTransform);

                var button = img.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                int index = i;
                button.onClick.AddListener(() => Select(index));

                var view = img.gameObject.AddComponent<CellView>();
                view.bg = img;
                view.text = text;
                view.Init(CellCenter(i / 9, i % 9));
                cells[i] = view;
            }

            var frame = UIKit.Box(grid, "SelectionFrame", UIKit.Accent, UIKit.SelectionFrame);
            selectionFrame = frame.rectTransform;
            selectionFrame.anchorMin = selectionFrame.anchorMax = UIKit.TopLeft;
            selectionFrame.pivot = UIKit.Center;
            selectionFrame.sizeDelta = new Vector2(CellSize + 24, CellSize + 24);
        }

        void BuildSidePanel(RectTransform root)
        {
            var side = UIKit.FramedPanel(root, "SidePanel", UIKit.SurfaceLight, 8, 28);
            UIKit.Place(side, UIKit.Center, new Vector2(435, -45), new Vector2(524, 812));
            var content = UIKit.Content(side);

            timeText = Plaque(content, "TIME", 14);
            mistakeText = Plaque(content, "MISTAKES", 178);
            scoreText = Plaque(content, "SCORE", 342);

            for (int d = 1; d <= 9; d++)
            {
                int digit = d;
                int row = (d - 1) / 3, col = (d - 1) % 3;
                var b = UIKit.MakeButton(content, d.ToString(), new Vector2(100, 100), UIKit.Blue, () => Enter(digit), 56);
                UIKit.Place((RectTransform)b.transform, UIKit.TopLeft, new Vector2(90 + col * 114, -(165 + row * 114)), new Vector2(100, 100));
                padButtons[d] = b;

                var count = UIKit.Label(UIKit.Content((RectTransform)b.transform), "", 22, new Color(0.58f, 0.64f, 0.82f, 0.9f),
                    TextAnchor.LowerRight, false);
                UIKit.Place(count.rectTransform, new Vector2(1, 0), new Vector2(-8, 4), new Vector2(40, 26));
                padCounts[d] = count;
            }

            var erase = UIKit.MakeButton(content, "Erase", new Vector2(328, 70), UIKit.Orange, Erase, 32);
            UIKit.Place((RectTransform)erase.transform, UIKit.TopLeft, new Vector2(90, -515), new Vector2(328, 70));

            var auto = UIKit.MakeButton(content, "Auto-Solve", new Vector2(328, 70), UIKit.Purple, AskAutoSolve, 32);
            UIKit.Place((RectTransform)auto.transform, UIKit.TopLeft, new Vector2(90, -600), new Vector2(328, 70));

            var menu = UIKit.MakeButton(content, "Leave Game", new Vector2(328, 70), UIKit.Slate, AskExit, 32);
            UIKit.Place((RectTransform)menu.transform, UIKit.TopLeft, new Vector2(90, -685), new Vector2(328, 70));
        }

        static Text Plaque(RectTransform parent, string caption, float x)
        {
            var p = UIKit.FramedPanel(parent, "Plaque_" + caption, UIKit.Surface, 4, 14);
            UIKit.Place(p, UIKit.TopLeft, new Vector2(x, -25), new Vector2(152, 110));
            var inner = UIKit.Content(p);
            var cap = UIKit.Label(inner, caption, 22, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(cap.rectTransform, UIKit.TopCenter, new Vector2(0, -8), new Vector2(140, 30));
            var value = UIKit.Label(inner, "", 40, UIKit.TextLight);
            UIKit.Place(value.rectTransform, UIKit.TopCenter, new Vector2(0, -42), new Vector2(140, 56));
            return value;
        }

        // ---------- Game flow ----------

        public void Begin(Difficulty d, string nickname)
        {
            CloseDialog();
            StopAllCoroutines();

            difficulty = d;
            puzzle = SudokuPuzzle.Generate(d);
            for (int i = 0; i < 81; i++)
            {
                values[i] = puzzle.Givens[i];
                isGiven[i] = values[i] != 0;
                autoFilled[i] = false;
            }
            elapsed = 0f;
            mistakes = 0;
            selected = -1;
            state = State.Playing;
            headerText.text = $"{nickname}  -  {d}";

            for (int i = 0; i < 81; i++)
            {
                if (isGiven[i]) continue;
                Select(i);
                selectionFrame.anchoredPosition = frameTarget;
                break;
            }
            Refresh();
        }

        void OnDisable()
        {
            CloseDialog();
        }

        void Update()
        {
            if (state == State.Playing) elapsed += Time.unscaledDeltaTime;

            timeText.text = ScoreRules.FormatTime(elapsed);
            mistakeText.text = mistakes.ToString();
            scoreText.text = state == State.Forfeited ? "0" : ScoreRules.Calculate(difficulty, elapsed, mistakes).ToString();

            bool showFrame = selected >= 0 && state == State.Playing;
            selectionFrame.gameObject.SetActive(showFrame);
            if (showFrame)
            {
                float k = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
                selectionFrame.anchoredPosition = Vector2.Lerp(selectionFrame.anchoredPosition, frameTarget, k);
                var c = UIKit.Accent;
                c.a = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
                selectionFrame.GetComponent<Image>().color = c;
            }

            if (state == State.Playing) HandleKeyboard();
        }

        void HandleKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame) { if (dialog == null) AskExit(); return; }
            if (dialog != null) return;

            for (int d = 1; d <= 9; d++)
            {
                if (kb[(Key)((int)Key.Digit1 + d - 1)].wasPressedThisFrame ||
                    kb[(Key)((int)Key.Numpad1 + d - 1)].wasPressedThisFrame)
                    Enter(d);
            }
            if (kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame) Erase();

            if (kb.upArrowKey.wasPressedThisFrame) Move(-1, 0);
            if (kb.downArrowKey.wasPressedThisFrame) Move(1, 0);
            if (kb.leftArrowKey.wasPressedThisFrame) Move(0, -1);
            if (kb.rightArrowKey.wasPressedThisFrame) Move(0, 1);
        }

        void Move(int dRow, int dCol)
        {
            if (selected < 0) { Select(40); return; }
            int row = Mathf.Clamp(selected / 9 + dRow, 0, 8);
            int col = Mathf.Clamp(selected % 9 + dCol, 0, 8);
            Select(row * 9 + col);
        }

        void Select(int index)
        {
            if (state != State.Playing || dialog != null) return;
            selected = index;
            frameTarget = CellCenter(index / 9, index % 9);
            Refresh();
        }

        void Enter(int digit)
        {
            if (state != State.Playing || dialog != null || selected < 0) return;
            if (isGiven[selected] || values[selected] == puzzle.Solution[selected]) return;

            values[selected] = digit;
            if (digit != puzzle.Solution[selected])
            {
                mistakes++;
                cells[selected].Shake();
            }
            Refresh();
            if (IsSolved()) StartCoroutine(WinRoutine());
        }

        void Erase()
        {
            if (state != State.Playing || dialog != null || selected < 0) return;
            if (isGiven[selected] || values[selected] == puzzle.Solution[selected]) return;
            values[selected] = 0;
            Refresh();
        }

        bool IsSolved()
        {
            for (int i = 0; i < 81; i++) if (values[i] != puzzle.Solution[i]) return false;
            return true;
        }

        GameResult MakeResult(bool forfeited)
        {
            return new GameResult
            {
                difficulty = difficulty,
                seconds = elapsed,
                mistakes = mistakes,
                forfeited = forfeited,
                score = forfeited ? 0 : ScoreRules.Calculate(difficulty, elapsed, mistakes)
            };
        }

        IEnumerator WinRoutine()
        {
            state = State.Won;
            var result = MakeResult(false);
            selected = -1;
            Refresh();

            for (int k = 0; k <= 16; k++)
            {
                for (int row = 0; row < 9; row++)
                {
                    int col = k - row;
                    if (col >= 0 && col < 9) cells[row * 9 + col].Pulse();
                }
                yield return new WaitForSecondsRealtime(0.05f);
            }
            yield return new WaitForSecondsRealtime(0.7f);
            Finished?.Invoke(result);
        }

        void AskAutoSolve()
        {
            if (state != State.Playing || dialog != null) return;
            dialog = UIKit.Confirm(transform, "Auto-Solve?",
                "Solving the puzzle for you forfeits ALL points for this game.",
                "Solve it", UIKit.Purple, () => { CloseDialog(); StartCoroutine(AutoSolveRoutine()); },
                "Keep playing", CloseDialog);
        }

        IEnumerator AutoSolveRoutine()
        {
            state = State.Forfeited;
            selected = -1;
            for (int i = 0; i < 81; i++)
            {
                if (values[i] == puzzle.Solution[i]) continue;
                values[i] = puzzle.Solution[i];
                autoFilled[i] = true;
                cells[i].Pulse();
                Refresh();
                yield return new WaitForSecondsRealtime(0.03f);
            }
            yield return new WaitForSecondsRealtime(1.2f);
            Finished?.Invoke(MakeResult(true));
        }

        void AskExit()
        {
            if (state != State.Playing || dialog != null) return;
            dialog = UIKit.Confirm(transform, "Leave Game?", "Your progress on this puzzle will be lost.",
                "Leave", UIKit.Red, () => { CloseDialog(); ExitRequested?.Invoke(); },
                "Stay", CloseDialog);
        }

        void CloseDialog()
        {
            if (dialog != null) Destroy(dialog);
            dialog = null;
        }

        // ---------- Visuals ----------

        bool IsWrong(int i) => !isGiven[i] && values[i] != 0 && values[i] != puzzle.Solution[i];

        static bool ArePeers(int a, int b)
        {
            int ra = a / 9, ca = a % 9, rb = b / 9, cb = b % 9;
            return ra == rb || ca == cb || (ra / 3 == rb / 3 && ca / 3 == cb / 3);
        }

        void Refresh()
        {
            int selectedValue = selected >= 0 ? values[selected] : 0;
            var counts = new int[10];

            for (int i = 0; i < 81; i++)
            {
                var cell = cells[i];
                bool wrong = IsWrong(i);
                if (values[i] != 0 && !wrong) counts[values[i]]++;

                var bg = isGiven[i] ? GivenColor : EmptyColor;
                if (selected >= 0)
                {
                    if (i == selected) bg = SelectedColor;
                    else
                    {
                        if (ArePeers(i, selected)) bg = Color.Lerp(bg, PeerTint, 0.45f);
                        if (selectedValue != 0 && values[i] == selectedValue && !wrong) bg = Color.Lerp(bg, SameTint, 0.7f);
                    }
                }
                if (wrong) bg = Color.Lerp(bg, UIKit.Red, 0.6f);
                cell.bg.color = bg;

                cell.text.text = values[i] == 0 ? "" : values[i].ToString();
                cell.text.color = wrong ? Color.white
                    : isGiven[i] ? UIKit.TextLight
                    : autoFilled[i] ? UIKit.Purple
                    : UIKit.Accent;
            }

            for (int d = 1; d <= 9; d++)
            {
                int left = 9 - counts[d];
                padCounts[d].text = left > 0 ? left.ToString() : "";
                padButtons[d].interactable = left > 0;
            }
        }
    }
}
