using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SudokuGame
{
    public class NicknameScreen : MonoBehaviour
    {
        public Action<string> Confirmed;
        public Action Cancelled;

        const int MaxLength = 14;
        const int MinLength = 2;

        InputField input;
        Text error;
        GameObject backButton;

        public static NicknameScreen Create(Transform parent)
        {
            var rt = UIKit.NewRect(parent, "NicknameScreen");
            var screen = rt.gameObject.AddComponent<NicknameScreen>();
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
            UIKit.Place(halo.rectTransform, UIKit.Center, new Vector2(0, 330), new Vector2(1000, 280));
            var title = UIKit.Label(root, "SUDOKU", 170, UIKit.TextLight);
            UIKit.Place(title.rectTransform, UIKit.Center, new Vector2(0, 330), new Vector2(1200, 220));

            var panel = UIKit.FramedPanel(root, "Panel", UIKit.SurfaceLight);
            UIKit.Place(panel, UIKit.Center, new Vector2(0, -90), new Vector2(900, 520));
            var content = UIKit.Content(panel);

            var heading = UIKit.Label(content, "Choose your nickname", 50, UIKit.TextLight);
            UIKit.Place(heading.rectTransform, UIKit.TopCenter, new Vector2(0, -35), new Vector2(800, 80));

            input = UIKit.MakeInput(content, new Vector2(640, 100), "Your name", MaxLength);
            UIKit.Place((RectTransform)input.transform, UIKit.TopCenter, new Vector2(0, -140), new Vector2(640, 100));
            input.onValidateInput += (text, index, ch) => IsAllowed(ch) ? ch : '\0';
            input.onEndEdit.AddListener(_ =>
            {
                var kb = Keyboard.current;
                if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) Submit();
            });

            error = UIKit.Label(content, "", 28, new Color(1f, 0.55f, 0.45f), TextAnchor.MiddleCenter, false);
            UIKit.Place(error.rectTransform, UIKit.TopCenter, new Vector2(0, -255), new Vector2(800, 40));

            var begin = UIKit.MakeButton(content, "Begin Adventure", new Vector2(460, 95), UIKit.Green, Submit, 40);
            UIKit.Place((RectTransform)begin.transform, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(460, 95));

            var back = UIKit.MakeButton(root, "Back", new Vector2(220, 75), UIKit.Slate, () => Cancelled?.Invoke(), 32);
            UIKit.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(40, 40), new Vector2(220, 75));
            ((RectTransform)back.transform).pivot = Vector2.zero;
            backButton = back.gameObject;
        }

        static bool IsAllowed(char c) => char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-';

        public void Show(string current, bool canCancel)
        {
            backButton.SetActive(canCancel);
            input.text = current ?? "";
            error.text = "";
            input.ActivateInputField();
        }

        void Submit()
        {
            string name = input.text.Trim();
            if (name.Length < MinLength)
            {
                error.text = $"Pick a nickname with at least {MinLength} characters.";
                return;
            }
            Confirmed?.Invoke(name);
        }
    }
}
