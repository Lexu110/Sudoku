using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SudokuGame
{
    /// <summary>Log in or create an account. The bootstrap performs the actual request.</summary>
    public class AuthScreen : MonoBehaviour
    {
        /// <summary>username, password, true when creating an account.</summary>
        public Action<string, string, bool> Submitted;

        const int UsernameMin = 3, UsernameMax = 20, PasswordMin = 8, PasswordMax = 128;

        InputField usernameInput, passwordInput;
        Text heading, status, submitLabel, switchLabel;
        Button submitButton, switchButton;
        bool registerMode;

        public static AuthScreen Create(Transform parent)
        {
            var rt = UIKit.NewRect(parent, "AuthScreen");
            var screen = rt.gameObject.AddComponent<AuthScreen>();
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

            var panel = UIKit.FramedPanel(root, "Panel", UIKit.SurfaceLight);
            UIKit.Place(panel, UIKit.Center, new Vector2(0, -130), new Vector2(900, 700));
            var content = UIKit.Content(panel);

            heading = UIKit.Label(content, "", 50, UIKit.TextLight);
            UIKit.Place(heading.rectTransform, UIKit.TopCenter, new Vector2(0, -30), new Vector2(800, 80));

            usernameInput = UIKit.MakeInput(content, new Vector2(700, 90), "Username", UsernameMax);
            UIKit.Place((RectTransform)usernameInput.transform, UIKit.TopCenter, new Vector2(0, -130), new Vector2(700, 90));
            usernameInput.onValidateInput += (text, index, ch) => IsUsernameChar(ch) ? ch : '\0';

            passwordInput = UIKit.MakeInput(content, new Vector2(700, 90), "Password", PasswordMax);
            UIKit.Place((RectTransform)passwordInput.transform, UIKit.TopCenter, new Vector2(0, -240), new Vector2(700, 90));
            passwordInput.contentType = InputField.ContentType.Password;

            usernameInput.onEndEdit.AddListener(_ => { if (EnterPressed()) passwordInput.ActivateInputField(); });
            passwordInput.onEndEdit.AddListener(_ => { if (EnterPressed()) Submit(); });

            status = UIKit.Label(content, "", 28, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(status.rectTransform, UIKit.TopCenter, new Vector2(0, -345), new Vector2(800, 80));

            submitButton = UIKit.MakeButton(content, "", new Vector2(460, 90), UIKit.Green, Submit, 40);
            UIKit.Place((RectTransform)submitButton.transform, new Vector2(0.5f, 0f), new Vector2(0, 130), new Vector2(460, 90));
            submitLabel = submitButton.GetComponentInChildren<Text>();

            switchButton = UIKit.MakeButton(content, "", new Vector2(460, 70), UIKit.Slate, ToggleMode, 28);
            UIKit.Place((RectTransform)switchButton.transform, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(460, 70));
            switchLabel = switchButton.GetComponentInChildren<Text>();

            ApplyMode();
        }

        static bool IsUsernameChar(char c) =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

        static bool EnterPressed()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame && usernameInput.isFocused) passwordInput.ActivateInputField();
        }

        void ToggleMode()
        {
            registerMode = !registerMode;
            ApplyMode();
            status.text = "";
        }

        void ApplyMode()
        {
            heading.text = registerMode ? "Create your account" : "Log in";
            submitLabel.text = registerMode ? "Create Account" : "Log In";
            switchLabel.text = registerMode ? "I already have an account" : "New here? Create an account";
        }

        /// <summary>Shows the form ready for input. message is optional (for example "Your session expired").</summary>
        public void Open(string message)
        {
            SetInteractable(true);
            passwordInput.text = "";
            SetStatus(message ?? "", false);
            usernameInput.ActivateInputField();
        }

        public void SetBusy(string message)
        {
            SetInteractable(false);
            SetStatus(message, false);
        }

        public void ShowError(string message)
        {
            SetInteractable(true);
            SetStatus(message, true);
        }

        void SetInteractable(bool value)
        {
            usernameInput.interactable = value;
            passwordInput.interactable = value;
            submitButton.interactable = value;
            switchButton.interactable = value;
        }

        void SetStatus(string message, bool isError)
        {
            status.text = message;
            status.color = isError ? new Color(1f, 0.55f, 0.5f) : UIKit.Muted;
        }

        void Submit()
        {
            if (!submitButton.interactable) return;

            string user = usernameInput.text.Trim();
            string pass = passwordInput.text;

            if (user.Length < UsernameMin || user.Length > UsernameMax)
            {
                SetStatus($"Username must be {UsernameMin}-{UsernameMax} characters (letters, numbers or _).", true);
                return;
            }
            if (registerMode && (pass.Length < PasswordMin || pass.Length > PasswordMax))
            {
                SetStatus($"Password must be at least {PasswordMin} characters.", true);
                return;
            }
            if (!registerMode && pass.Length == 0)
            {
                SetStatus("Enter your password.", true);
                return;
            }
            Submitted?.Invoke(user, pass, registerMode);
        }
    }
}
