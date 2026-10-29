using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Game.Ads
{
    /// <summary>
    /// Builds the ad-related UI entirely from code at runtime.
    ///
    /// Why code and not prefabs: the game's scene is authored in the Unity Editor, and ad UI
    /// added here would otherwise require opening the Editor and wiring prefabs. Building it in
    /// code keeps the scene untouched, so the project still builds correctly from a clean clone.
    ///
    /// Nothing here references TextMeshPro, so it cannot break if TMP essentials are missing.
    /// </summary>
    public sealed class AdsRuntimeUI : MonoBehaviour
    {
        public static AdsRuntimeUI Instance { get; private set; }

        private const int ReferenceWidth = 1080;
        private const int ReferenceHeight = 1920;

        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color CardColor = new Color(0.13f, 0.14f, 0.18f, 1f);
        private static readonly Color AcceptColor = new Color(0.18f, 0.72f, 0.36f, 1f);
        private static readonly Color DeclineColor = new Color(0.30f, 0.31f, 0.36f, 1f);
        private static readonly Color TextColor = Color.white;

        private Canvas _canvas;
        private GameObject _promptRoot;
        private GameObject _doubleCoinsRoot;
        private Font _font;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureEventSystem();
            BuildCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #region Setup

        private void EnsureEventSystem()
        {
            // Most scenes already have one; add it only when missing so buttons stay clickable.
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("[AdsEventSystem]");
            DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            // This project runs on the new Input System (Player Settings > Active Input Handling),
            // so the legacy StandaloneInputModule would throw at runtime. Use the matching module.
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private void BuildCanvas()
        {
            var go = new GameObject("[AdsCanvas]");
            go.transform.SetParent(transform, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }

        private Font UiFont
        {
            get
            {
                if (_font != null) return _font;

                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch { /* fall through */ }

                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                    catch { /* fall through */ }
                }

                if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return _font;
            }
        }

        #endregion

        #region Builder helpers

        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private Image NewPanel(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        private Text NewText(string name, Transform parent, string value, int size, TextAnchor anchor)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiFont;
            t.text = value;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = TextColor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, Color bg, Action onClick)
        {
            var rt = NewRect(name, parent);

            // White rounded rim, matching the chunky style of the game's own buttons.
            var rim = rt.gameObject.AddComponent<Image>();
            rim.sprite = RoundedSprite;
            rim.type = Image.Type.Sliced;
            rim.color = new Color(1f, 1f, 1f, 0.95f);
            rim.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = rim;

            // Coloured face sits inside the rim, so the outline reads as a border.
            var faceRt = NewRect("Face", rt);
            Stretch(faceRt);
            faceRt.offsetMin = new Vector2(5f, 5f);
            faceRt.offsetMax = new Vector2(-5f, -5f);
            var face = faceRt.gameObject.AddComponent<Image>();
            face.sprite = RoundedSprite;
            face.type = Image.Type.Sliced;
            face.color = bg;
            face.raycastTarget = false;

            var text = NewText(name + "Label", rt, label, 40, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            var trt = text.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 0f);
            trt.offsetMax = new Vector2(-14f, 0f);

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>
        /// A 9-sliced rounded rectangle generated once at runtime. The ad UI is built from
        /// code (no prefabs), so without this every button is a hard-edged rectangle that
        /// looks nothing like the rest of the game.
        /// </summary>
        private static Sprite _roundedSprite;

        private static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite != null) return _roundedSprite;

                const int size = 64;
                const float radius = 22f;

                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "AdRoundedRect"
                };

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Distance from the nearest corner-circle centre; inside the
                        // straight middle section the clamp makes it 0 -> fully opaque.
                        float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                        float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        float a = Mathf.Clamp01(radius - d + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();

                float b = radius + 1f;
                _roundedSprite = Sprite.Create(
                    tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
                return _roundedSprite;
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion

        #region Extra life prompt

        /// <summary>
        /// Shows the "out of lives — watch a video for +1 life?" prompt.
        /// Blocks the playfield behind it until the player chooses.
        /// </summary>
        public static void ShowExtraLifePrompt(Action onAccept, Action onDecline)
        {
            if (Instance == null)
            {
                onDecline?.Invoke();
                return;
            }
            Instance.BuildExtraLifePrompt(onAccept, onDecline);
        }

        private void BuildExtraLifePrompt(Action onAccept, Action onDecline)
        {
            HideExtraLifePrompt();

            _promptRoot = new GameObject("ExtraLifePrompt", typeof(RectTransform));
            var rootRt = (RectTransform)_promptRoot.transform;
            rootRt.SetParent(_canvas.transform, false);
            Stretch(rootRt);

            // Full-screen dim that swallows taps meant for the playfield.
            var dim = NewPanel("Dim", _promptRoot.transform, DimColor);
            Stretch(dim.rectTransform);

            var card = NewPanel("Card", _promptRoot.transform, CardColor);
            card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.rectTransform.sizeDelta = new Vector2(820f, 560f);
            card.rectTransform.anchoredPosition = Vector2.zero;

            // Same rounded treatment as the buttons; sharp-cornered cards look off-brand.
            card.sprite = RoundedSprite;
            card.type = Image.Type.Sliced;

            var title = NewText("Title", card.transform, "OUT OF LIVES", 58, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(-80f, 90f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -40f);

            var body = NewText("Body", card.transform,
                "Watch a short video to get\n+1 life and keep playing.", 34, TextAnchor.MiddleCenter);
            body.rectTransform.anchorMin = new Vector2(0f, 0.47f);
            body.rectTransform.anchorMax = new Vector2(1f, 0.75f);
            body.rectTransform.offsetMin = new Vector2(60f, 0f);
            body.rectTransform.offsetMax = new Vector2(-60f, 0f);
            body.color = new Color(0.85f, 0.86f, 0.9f, 1f);

            var accept = NewButton("Accept", card.transform, "WATCH AD  +1 LIFE", AcceptColor, () =>
            {
                HideExtraLifePrompt();
                onAccept?.Invoke();
            });
            accept.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            accept.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            accept.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            accept.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 105f);
            accept.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 145f);

            var decline = NewButton("Decline", card.transform, "NO THANKS", DeclineColor, () =>
            {
                HideExtraLifePrompt();
                onDecline?.Invoke();
            });
            decline.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            decline.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            decline.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            decline.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 90f);
            decline.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 40f);
        }

        public static void HideExtraLifePrompt()
        {
            if (Instance == null || Instance._promptRoot == null) return;
            Destroy(Instance._promptRoot);
            Instance._promptRoot = null;
        }

        #endregion

        #region Double coins button

        /// <summary>
        /// Shows the "double your coins" rewarded button while the win panel is up.
        /// Safe-area aware: sits above the system gesture bar.
        /// </summary>
        public static void ShowDoubleCoinsButton(int bonus, Action<int> onRewarded)
        {
            if (Instance == null) return;
            Instance.BuildDoubleCoinsButton(bonus, onRewarded);
        }

        private void BuildDoubleCoinsButton(int bonus, Action<int> onRewarded)
        {
            HideDoubleCoinsButton();

            _doubleCoinsRoot = new GameObject("DoubleCoinsButton", typeof(RectTransform));
            var rt = (RectTransform)_doubleCoinsRoot.transform;
            rt.SetParent(_canvas.transform, false);

            float bottomInset = 140f + Screen.safeArea.y;
            float scale = ReferenceHeight / Mathf.Max(1f, Screen.height);
            bottomInset *= scale;

            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(620f, 110f);
            rt.anchoredPosition = new Vector2(0f, bottomInset);

            // Match game UI: dark card style with subtle accent instead of bright green
            var btn = NewButton("Double", rt, "WATCH AD  x2 COINS", AcceptColor, () =>
            {
                HideDoubleCoinsButton();
                AdsManager.Instance?.ShowRewarded(
                    onRewarded: () =>
                    {
                        AdsManager.Instance.UnlockDoubleCoins();
                        onRewarded?.Invoke(bonus * AdsConfig.DoubleCoinsMultiplier);
                    },
                    onUnavailable: () => { },
                    unitId: AdsConfig.RewardedDoubleCoinsUnitId);
            });

            // Keep green accept color matching life-prompt button.
            // The label lives inside the button itself ("WATCH AD  x2 COINS"), so no
            // extra caption is drawn here — a second label used to sit *behind* the
            // button and leaked out below it as a ghost "2x COINS" line.
            btn.transform.SetAsLastSibling();

            // NewButton creates its own 100x100 root. Stretch it over the
            // 620x110 bar we just created, otherwise the label overflows a
            // tiny button in the middle of the bar.
            RectTransform btnRect = btn.GetComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
        }

        public static void HideDoubleCoinsButton()
        {
            if (Instance == null || Instance._doubleCoinsRoot == null) return;
            Destroy(Instance._doubleCoinsRoot);
            Instance._doubleCoinsRoot = null;
        }

        #endregion

        public static void HideAll()
        {
            HideExtraLifePrompt();
            HideDoubleCoinsButton();
        }
    }
}
