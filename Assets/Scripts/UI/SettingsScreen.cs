using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SudokuGame
{
    public class SettingsScreen : MonoBehaviour
    {
        public Action Back;

        Text[,] keyLabels;
        Text message;
        int listenAction = -1, listenSlot = -1;
        float lastPreview;

        const string Hint = "Click a key, then press the new one. Number keys 1-9 are fixed.";

        public static SettingsScreen Create(Transform parent)
        {
            var rt = UIKit.NewRect(parent, "SettingsScreen");
            var screen = rt.gameObject.AddComponent<SettingsScreen>();
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

            var title = UIKit.Label(content, "Settings", 60, UIKit.TextLight);
            UIKit.Place(title.rectTransform, UIKit.TopCenter, new Vector2(0, -15), new Vector2(900, 80));

            SectionLabel(content, "Audio", -100);
            AudioRow(content, "Music", -145, AudioManager.MusicVolume, v => AudioManager.MusicVolume = v);
            AudioRow(content, "Sound Effects", -215, AudioManager.SfxVolume, v =>
            {
                AudioManager.SfxVolume = v;
                if (Time.unscaledTime - lastPreview < 0.12f) return;
                lastPreview = Time.unscaledTime;
                AudioManager.Play(Sfx.Select);
            });

            SectionLabel(content, "Key Bindings", -300);
            Header(content, "Action", 60, 360, TextAnchor.MiddleLeft);
            Header(content, "Primary", 470, 260, TextAnchor.MiddleCenter);
            Header(content, "Alternate", 760, 260, TextAnchor.MiddleCenter);

            keyLabels = new Text[KeyBindings.ActionCount, KeyBindings.Slots];
            for (int a = 0; a < KeyBindings.ActionCount; a++)
            {
                float y = -390 - a * 66;
                var name = UIKit.Label(content, KeyBindings.ActionName((GameAction)a), 34, UIKit.TextLight, TextAnchor.MiddleLeft, false);
                UIKit.Place(name.rectTransform, UIKit.TopLeft, new Vector2(60, y), new Vector2(360, 56));

                for (int s = 0; s < KeyBindings.Slots; s++)
                {
                    int action = a, slot = s;
                    var b = UIKit.MakeButton(content, "", new Vector2(260, 56), UIKit.Blue, () => ToggleListening(action, slot), 30);
                    UIKit.Place((RectTransform)b.transform, UIKit.TopLeft, new Vector2(470 + s * 290, y), new Vector2(260, 56));
                    keyLabels[a, s] = b.GetComponentInChildren<Text>();
                }
            }

            message = UIKit.Label(content, Hint, 26, UIKit.Muted, TextAnchor.MiddleCenter, false);
            UIKit.Place(message.rectTransform, UIKit.TopCenter, new Vector2(0, -795), new Vector2(1000, 50));

            var reset = UIKit.MakeButton(root, "Reset Defaults", new Vector2(320, 80), UIKit.Orange, ResetDefaults, 32);
            UIKit.Place((RectTransform)reset.transform, new Vector2(0.5f, 0f), new Vector2(-190, 40), new Vector2(320, 80));
            var back = UIKit.MakeButton(root, "Back", new Vector2(320, 80), UIKit.Slate, () => Back?.Invoke(), 36);
            UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(190, 40), new Vector2(320, 80));
        }

        static void SectionLabel(RectTransform parent, string text, float y)
        {
            var t = UIKit.Label(parent, text, 34, UIKit.Accent, TextAnchor.MiddleLeft, false);
            UIKit.Place(t.rectTransform, UIKit.TopLeft, new Vector2(60, y), new Vector2(600, 44));
        }

        static void AudioRow(RectTransform parent, string caption, float y, float value, Action<float> onChanged)
        {
            var label = UIKit.Label(parent, caption, 34, UIKit.TextLight, TextAnchor.MiddleLeft, false);
            UIKit.Place(label.rectTransform, UIKit.TopLeft, new Vector2(60, y - 8), new Vector2(380, 56));
            var slider = UIKit.MakeSlider(parent, new Vector2(550, 56), value, onChanged);
            UIKit.Place((RectTransform)slider.transform, UIKit.TopLeft, new Vector2(470, y - 8), new Vector2(550, 56));
        }

        static void Header(RectTransform parent, string text, float x, float width, TextAnchor anchor)
        {
            var t = UIKit.Label(parent, text, 30, UIKit.Muted, anchor, false);
            UIKit.Place(t.rectTransform, UIKit.TopLeft, new Vector2(x, -345), new Vector2(width, 40));
        }

        void OnEnable()
        {
            StopListening();
            Refresh();
        }

        void OnDisable()
        {
            listenAction = listenSlot = -1;
        }

        void ToggleListening(int action, int slot)
        {
            if (listenAction == action && listenSlot == slot)
            {
                StopListening();
                return;
            }
            listenAction = action;
            listenSlot = slot;
            message.text = $"Press a key for {KeyBindings.ActionName((GameAction)action)}...";
            Refresh();
        }

        void StopListening()
        {
            listenAction = listenSlot = -1;
            if (message != null) message.text = Hint;
            Refresh();
        }

        void ResetDefaults()
        {
            KeyBindings.ResetToDefaults();
            StopListening();
            message.text = "Key bindings reset to defaults.";
        }

        void Update()
        {
            if (listenAction < 0) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            foreach (var control in kb.allKeys)
            {
                if (!control.wasPressedThisFrame) continue;
                var key = control.keyCode;
                if (key == Key.None || key == Key.IMESelected) continue;

                if (KeyBindings.IsReserved(key))
                {
                    message.text = "Number keys 1-9 are used for entering numbers. Pick another key.";
                    return;
                }

                var action = (GameAction)listenAction;
                KeyBindings.Set(action, listenSlot, key);
                listenAction = listenSlot = -1;
                Refresh();
                message.text = $"{KeyBindings.ActionName(action)} is now {KeyBindings.KeyName(key)}.";
                return;
            }
        }

        void Refresh()
        {
            if (keyLabels == null) return;
            for (int a = 0; a < KeyBindings.ActionCount; a++)
            for (int s = 0; s < KeyBindings.Slots; s++)
            {
                bool listening = a == listenAction && s == listenSlot;
                keyLabels[a, s].text = listening ? "Press a key..." : KeyBindings.KeyName(KeyBindings.Get((GameAction)a, s));
                keyLabels[a, s].color = listening ? UIKit.Accent : Color.white;
            }
        }
    }
}
