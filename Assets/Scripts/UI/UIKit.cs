using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SudokuGame
{
    /// <summary>
    /// Builds the whole look from code: procedural sprites (no art files needed),
    /// a dark neon-accent theme with flat rounded cards and buttons.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Accent = new Color(0.30f, 0.85f, 0.98f);
        public static readonly Color Muted = new Color(0.58f, 0.64f, 0.82f);
        public static readonly Color Surface = new Color(0.06f, 0.07f, 0.14f);
        public static readonly Color SurfaceLight = new Color(0.11f, 0.13f, 0.25f);
        public static readonly Color Slate = new Color(0.25f, 0.29f, 0.45f);
        public static readonly Color Orange = new Color(0.98f, 0.58f, 0.18f);
        public static readonly Color Red = new Color(0.96f, 0.28f, 0.38f);
        public static readonly Color Green = new Color(0.14f, 0.74f, 0.48f);
        public static readonly Color Blue = new Color(0.26f, 0.47f, 0.98f);
        public static readonly Color Purple = new Color(0.58f, 0.40f, 0.98f);
        public static readonly Color TextLight = new Color(0.93f, 0.95f, 1f);

        // ---------- Fonts ----------

        static Font font;

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Helvetica Neue", "Segoe UI", "Inter", "Roboto", "Ubuntu", "Arial", "DejaVu Sans", "Liberation Sans" }, 32);
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        // ---------- Procedural sprites ----------

        static readonly Dictionary<int, Sprite> roundedCache = new Dictionary<int, Sprite>();
        static Sprite glow, backdrop;

        static Sprite MakeSprite(Texture2D tex, Vector4 border, float ppu = 100f)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0,
                SpriteMeshType.FullRect, border);
        }

        /// <summary>White rounded rectangle meant for Image.Type.Sliced. Tint it with Image.color.</summary>
        public static Sprite Rounded(int radius)
        {
            if (roundedCache.TryGetValue(radius, out var cached)) return cached;
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float inner = half - radius;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - inner;
                float qy = Mathf.Abs(y + 0.5f - half) - inner;
                float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
            var sprite = MakeSprite(tex, new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
            roundedCache[radius] = sprite;
            return sprite;
        }

        /// <summary>Soft white halo, sliced. Used behind selected cells and the title.</summary>
        public static Sprite Glow
        {
            get
            {
                if (glow != null) return glow;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Max(Mathf.Abs(x + 0.5f - 32) - 12, 0);
                    float qy = Mathf.Max(Mathf.Abs(y + 0.5f - 32) - 12, 0);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - new Vector2(qx, qy).magnitude / 20f), 2f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                glow = MakeSprite(tex, new Vector4(32, 32, 32, 32));
                return glow;
            }
        }

        static Sprite selectionFrame;

        /// <summary>
        /// Gold ring with a soft outer halo and a transparent middle, sliced. The ring sits 12px inside the sprite edge,
        /// so size the Image 24px larger than the thing it surrounds.
        /// </summary>
        public static Sprite SelectionFrame
        {
            get
            {
                if (selectionFrame != null) return selectionFrame;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - 32) - 10f;
                    float qy = Mathf.Abs(y + 0.5f - 32) - 10f;
                    float sd = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - 10f;
                    float a;
                    if (sd > 0f) a = Mathf.Pow(Mathf.Clamp01(1f - sd / 12f), 2f) * 0.85f;
                    else a = Mathf.Clamp01(sd + 4f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                selectionFrame = MakeSprite(tex, new Vector4(32, 32, 32, 32));
                return selectionFrame;
            }
        }

        /// <summary>Deep navy gradient with soft violet and teal light pools.</summary>
        public static Sprite Backdrop
        {
            get
            {
                if (backdrop != null) return backdrop;
                const int size = 512;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var top = new Color(0.05f, 0.06f, 0.13f);
                var bottom = new Color(0.10f, 0.08f, 0.22f);
                var violet = new Color(0.40f, 0.20f, 0.75f);
                var teal = new Color(0.08f, 0.55f, 0.65f);

                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1), v = y / (float)(size - 1);
                    var c = Color.Lerp(bottom, top, v);
                    float dViolet = Mathf.Clamp01(1f - new Vector2(u - 0.12f, v - 0.92f).magnitude / 0.75f);
                    float dTeal = Mathf.Clamp01(1f - new Vector2(u - 0.92f, v - 0.08f).magnitude / 0.7f);
                    c += violet * (dViolet * dViolet * 0.55f) + teal * (dTeal * dTeal * 0.45f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
                backdrop = MakeSprite(tex, Vector4.zero);
                return backdrop;
            }
        }

        // ---------- Layout helpers ----------

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors and pivots the rect to the same point (0..1 of the parent) and sets position and size.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);

        // ---------- Widgets ----------

        public static Image Box(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = NewRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = false)
        {
            var rt = NewRect(parent, "Label");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontStyle = FontStyle.Bold;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0.05f, 0.6f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        /// <summary>Flat rounded card with a faint light edge and soft shadow. Put children into Content(returned rect).</summary>
        public static RectTransform FramedPanel(Transform parent, string name, Color fill, int rim = 2, int radius = 24)
        {
            const int edge = 2;
            var outer = Box(parent, name, new Color(1, 1, 1, 0.14f), Rounded(radius), true);
            var shadow = outer.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(0, -8);

            var inner = Box(outer.transform, "Fill", fill, Rounded(Mathf.Max(radius - edge, 4)));
            Stretch(inner.rectTransform, edge, edge, edge, edge);
            return outer.rectTransform;
        }

        public static RectTransform Content(RectTransform framedPanel) => (RectTransform)framedPanel.Find("Fill");

        public static Button MakeButton(Transform parent, string label, Vector2 size, Color fill, Action onClick, int fontSize = 34)
        {
            var panel = FramedPanel(parent, "Button_" + label, fill, 2, 20);
            panel.sizeDelta = size;
            var inner = Content(panel).GetComponent<Image>();

            var text = Label(inner.transform, label, fontSize, Color.white);
            Stretch(text.rectTransform);

            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = inner;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.15f, 1.15f, 1.15f),
                pressedColor = new Color(0.8f, 0.8f, 0.8f),
                selectedColor = new Color(1.15f, 1.15f, 1.15f),
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.8f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            if (onClick != null) button.onClick.AddListener(() => onClick());
            button.onClick.AddListener(() => AudioManager.Play(Sfx.Click));
            panel.gameObject.AddComponent<ButtonJuice>();
            return button;
        }

        /// <summary>Horizontal 0..1 slider built from code. onChanged is called while dragging.</summary>
        public static Slider MakeSlider(Transform parent, Vector2 size, float value, Action<float> onChanged)
        {
            var root = NewRect(parent, "Slider");
            root.sizeDelta = size;
            var slider = root.gameObject.AddComponent<Slider>();

            var track = Box(root, "Track", Surface, Rounded(8));
            track.rectTransform.anchorMin = new Vector2(0, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0, 16);

            var fillArea = NewRect(root, "Fill Area");
            fillArea.anchorMin = new Vector2(0, 0.5f);
            fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(0, 16);
            var fill = Box(fillArea, "Fill", Accent, Rounded(8));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = NewRect(root, "Handle Slide Area");
            Stretch(handleArea, 18, 18, 0, 0);
            var handle = Box(handleArea, "Handle", Color.white, Rounded(18), true);
            handle.rectTransform.sizeDelta = new Vector2(36, 36);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return slider;
        }

        public static InputField MakeInput(Transform parent, Vector2 size, string placeholder, int maxLength)
        {
            var panel = FramedPanel(parent, "TextInput", Surface, 2, 20);
            panel.sizeDelta = size;
            var inner = Content(panel);

            var text = Label(inner, "", 44, TextLight, TextAnchor.MiddleCenter, false);
            Stretch(text.rectTransform, 20, 20, 6, 6);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var hint = Label(inner, placeholder, 40, new Color(0.58f, 0.64f, 0.82f, 0.5f), TextAnchor.MiddleCenter, false);
            hint.fontStyle = FontStyle.Normal;
            Stretch(hint.rectTransform, 20, 20, 6, 6);

            var field = panel.gameObject.AddComponent<InputField>();
            field.targetGraphic = inner.GetComponent<Image>();
            field.textComponent = text;
            field.placeholder = hint;
            field.characterLimit = maxLength;
            field.customCaretColor = true;
            field.caretColor = Accent;
            field.selectionColor = new Color(0.3f, 0.85f, 0.98f, 0.35f);
            return field;
        }

        public static void ScreenBackground(Transform parent)
        {
            var bg = Box(parent, "Backdrop", Color.white);
            bg.sprite = Backdrop;
            Stretch(bg.rectTransform);
            var motes = NewRect(parent, "Motes");
            Stretch(motes);
            motes.gameObject.AddComponent<FloatingMotes>();
        }

        /// <summary>Modal yes/no dialog. Returns the root so the caller can destroy it.</summary>
        public static GameObject Confirm(Transform canvasRoot, string title, string message,
            string yesLabel, Color yesColor, Action onYes, string noLabel, Action onNo)
        {
            var dim = Box(canvasRoot, "Dialog", new Color(0.02f, 0.02f, 0.08f, 0.75f), null, true);
            Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<CanvasGroup>();
            dim.gameObject.AddComponent<PopIn>();

            var panel = FramedPanel(dim.transform, "Panel", SurfaceLight);
            Place(panel, Center, Vector2.zero, new Vector2(820, 420));
            var content = Content(panel);

            var t = Label(content, title, 52, Accent);
            Place(t.rectTransform, TopCenter, new Vector2(0, -30), new Vector2(740, 70));
            var m = Label(content, message, 32, TextLight, TextAnchor.MiddleCenter, false);
            Place(m.rectTransform, TopCenter, new Vector2(0, -110), new Vector2(720, 130));

            var yes = MakeButton(content, yesLabel, new Vector2(300, 80), yesColor, () => { onYes?.Invoke(); }, 32);
            Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(-170, 40), new Vector2(300, 80));
            var no = MakeButton(content, noLabel, new Vector2(300, 80), Blue, () => { onNo?.Invoke(); }, 32);
            Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(170, 40), new Vector2(300, 80));
            return dim.gameObject;
        }
    }

    /// <summary>Hover grows the button a little, pressing shrinks it.</summary>
    public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        bool hover, down, selected;
        public bool Highlighted;

        public void OnSelect(BaseEventData e) => selected = true;
        public void OnDeselect(BaseEventData e) => selected = false;

        public void OnPointerEnter(PointerEventData e)
        {
            hover = true;
            AudioManager.Play(Sfx.Hover);
        }
        public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
        public void OnPointerDown(PointerEventData e) => down = true;
        public void OnPointerUp(PointerEventData e) => down = false;

        void OnDisable()
        {
            hover = down = selected = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float target = down ? 0.95f : (hover || selected || Highlighted) ? 1.06f : 1f;
            float k = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, k);
        }
    }

    /// <summary>Fades and scales a UI object in when it is enabled. Needs a CanvasGroup on the same object.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PopIn : MonoBehaviour
    {
        CanvasGroup group;
        float t;

        void OnEnable()
        {
            group = GetComponent<CanvasGroup>();
            t = 0f;
            group.alpha = 0f;
            transform.localScale = Vector3.one * 0.94f;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.25f);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            group.alpha = e;
            transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
        }
    }

    /// <summary>Large soft colored orbs drifting slowly upwards behind the UI.</summary>
    public class FloatingMotes : MonoBehaviour
    {
        class Mote { public RectTransform rt; public Image img; public float speed, phase, sway, baseAlpha; }

        readonly List<Mote> motes = new List<Mote>();
        RectTransform area;

        void Start()
        {
            area = (RectTransform)transform;
            var tints = new[] { UIKit.Accent, UIKit.Purple, UIKit.Blue };
            for (int i = 0; i < 12; i++)
            {
                var img = UIKit.Box(transform, "Mote", Color.white, UIKit.Glow);
                float size = UnityEngine.Random.Range(180f, 480f);
                img.rectTransform.sizeDelta = new Vector2(size, size);
                var m = new Mote
                {
                    rt = img.rectTransform,
                    img = img,
                    speed = UnityEngine.Random.Range(8f, 22f),
                    phase = UnityEngine.Random.value * 10f,
                    sway = UnityEngine.Random.Range(10f, 40f),
                    baseAlpha = UnityEngine.Random.Range(0.05f, 0.12f)
                };
                var tint = tints[i % tints.Length];
                tint.a = m.baseAlpha;
                img.color = tint;
                var r = AreaRect();
                img.rectTransform.anchoredPosition = new Vector2(
                    UnityEngine.Random.Range(r.xMin, r.xMax), UnityEngine.Random.Range(r.yMin, r.yMax));
                motes.Add(m);
            }
        }

        Rect AreaRect()
        {
            var r = area.rect;
            return r.width < 1f ? new Rect(-960, -540, 1920, 1080) : r;
        }

        void Update()
        {
            var r = AreaRect();
            foreach (var m in motes)
            {
                var p = m.rt.anchoredPosition;
                p.y += m.speed * Time.unscaledDeltaTime;
                p.x += Mathf.Sin(Time.unscaledTime * 0.5f + m.phase) * m.sway * Time.unscaledDeltaTime;
                if (p.y > r.yMax + 300f)
                {
                    p.y = r.yMin - 300f;
                    p.x = UnityEngine.Random.Range(r.xMin, r.xMax);
                }
                m.rt.anchoredPosition = p;
            }
        }
    }
}
