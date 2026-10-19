using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SerapKeremGameKit._InputSystem;
using SerapKeremGameKit._Managers;

namespace _Game.UI
{
    /// <summary>
    /// Runtime-built level select overlay. The game has no Editor-authored
    /// level menu, so this builds the whole screen from the existing HUD font
    /// and a themed sprite. Unlocked levels are the only clickable tiles.
    /// </summary>
    public sealed class LevelSelectMenu : MonoBehaviour
    {
        private const int LevelsPerPage = 50;
        private const int Columns = 5;
        private const int Rows = 10;

        private static readonly Color Navy = new Color(0.055f, 0.165f, 0.278f, 1f);
        private static readonly Color DeepNavy = new Color(0.025f, 0.075f, 0.125f, 0.96f);
        private static readonly Color Blue = new Color(0.196f, 0.545f, 0.953f, 1f);
        private static readonly Color Cream = new Color(0.969f, 0.933f, 0.855f, 1f);
        private static readonly Color Gold = new Color(0.957f, 0.714f, 0.247f, 1f);
        private static readonly Color Slate = new Color(0.345f, 0.420f, 0.520f, 1f);
        private static readonly Color MutedText = new Color(0.88f, 0.89f, 0.91f, 0.58f);

        private sealed class Tile
        {
            public Button Button;
            public Image Background;
            public TextMeshProUGUI Number;
            public TextMeshProUGUI State;
            public int Level;
        }

        private readonly List<Tile> _tiles = new();
        private TMP_FontAsset _font;
        private Sprite _tileSprite;
        private bool _built;
        private bool _isOpen;
        private int _page;
        private int _unlocked;
        private int _active;
        private float _previousTimeScale = 1f;

        private TextMeshProUGUI _progressText;
        private TextMeshProUGUI _pageText;
        private Button _previousButton;
        private Button _nextButton;

        public static LevelSelectMenu Create(Transform canvasRoot, TMP_FontAsset font, Sprite tileSprite)
        {
            GameObject go = new GameObject("LevelSelectMenu", typeof(RectTransform), typeof(LevelSelectMenu));
            go.transform.SetParent(canvasRoot, false);
            LevelSelectMenu menu = go.GetComponent<LevelSelectMenu>();
            menu.Initialize(font, tileSprite);
            return menu;
        }

        public event Action LevelSelected;

        public void Initialize(TMP_FontAsset font, Sprite tileSprite)
        {
            if (_built) return;

            _font = font != null ? font : TMP_Settings.defaultFontAsset;
            _tileSprite = tileSprite;
            Build();
            _built = true;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            if (!_built || _isOpen) return;

            _isOpen = true;
            _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            InputHandler.Instance?.LockInput();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            _page = Mathf.Clamp((LevelManager.Instance.ActiveLevelNumber - 1) / LevelsPerPage, 0, PageCount - 1);
            Refresh();
        }

        public void Hide()
        {
            if (!_isOpen) return;

            _isOpen = false;
            gameObject.SetActive(false);
            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            InputHandler.Instance?.UnlockInput();
        }

        private void Update()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        private void OnDisable()
        {
            // If the canvas or scene is disabled while the overlay is open,
            // make sure the game is not left frozen at timeScale 0.
            if (!_isOpen) return;

            _isOpen = false;
            Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;
            InputHandler.Instance?.UnlockInput();
        }

        private int TotalLevels => Mathf.Max(1, LevelManager.Instance.TotalLevelCount);
        private int PageCount => Mathf.Max(1, Mathf.CeilToInt(TotalLevels / (float)LevelsPerPage));

        private void Build()
        {
            RectTransform root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Image backdrop = gameObject.AddComponent<Image>();
            backdrop.color = DeepNavy;
            backdrop.raycastTarget = true;

            Button backdropButton = gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = backdrop;
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(Hide);

            RectTransform shadow = NewRect("PanelShadow", transform);
            shadow.anchorMin = shadow.anchorMax = new Vector2(0.5f, 0.5f);
            shadow.pivot = new Vector2(0.5f, 0.5f);
            shadow.sizeDelta = new Vector2(960f, 1520f);
            shadow.anchoredPosition = new Vector2(0f, -12f);
            Image shadowImage = shadow.gameObject.AddComponent<Image>();
            shadowImage.color = new Color(0f, 0f, 0f, 0.35f);
            shadowImage.raycastTarget = false;
            SetSliced(shadowImage);

            RectTransform panel = NewRect("Panel", transform);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(940f, 1500f);
            panel.anchoredPosition = Vector2.zero;
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = Cream;
            panelImage.raycastTarget = true;
            SetSliced(panelImage);

            // Clicks on the panel background must not bubble up to the
            // full-screen backdrop button and close the menu. This Button has
            // no listener; it only consumes the click.
            Button panelBlocker = panel.gameObject.AddComponent<Button>();
            panelBlocker.targetGraphic = panelImage;
            panelBlocker.transition = Selectable.Transition.None;
            panelBlocker.navigation = new Navigation { mode = Navigation.Mode.None };

            Outline panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.75f);
            panelOutline.effectDistance = new Vector2(3f, -3f);

            TextMeshProUGUI title = NewText("Title", panel, "LEVEL SELECT", 54f, Navy, TextAlignmentOptions.Center, FontStyles.Bold);
            SetTop(title.rectTransform, 34f, 88f, 40f);

            TextMeshProUGUI subtitle = NewText("Subtitle", panel, "Choose any unlocked level", 26f,
                new Color(0.33f, 0.42f, 0.50f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
            SetTop(subtitle.rectTransform, 116f, 44f, 70f);

            Image rule = NewImage("Rule", panel, Gold);
            RectTransform ruleRt = rule.rectTransform;
            ruleRt.anchorMin = ruleRt.anchorMax = new Vector2(0.5f, 1f);
            ruleRt.pivot = new Vector2(0.5f, 1f);
            ruleRt.sizeDelta = new Vector2(170f, 5f);
            ruleRt.anchoredPosition = new Vector2(0f, -166f);

            _progressText = NewText("Progress", panel, "", 23f, Blue, TextAlignmentOptions.Center, FontStyles.Bold);
            SetTop(_progressText.rectTransform, 176f, 38f, 70f);

            Button close = NewButton("Close", panel, "×", 76f, 76f, Blue, Cream, Hide);
            RectTransform closeRt = close.GetComponent<RectTransform>();
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(76f, 76f);
            closeRt.anchoredPosition = new Vector2(-26f, -26f);

            RectTransform grid = NewRect("Grid", panel);
            grid.anchorMin = Vector2.zero;
            grid.anchorMax = Vector2.one;
            grid.offsetMin = new Vector2(40f, 190f);
            grid.offsetMax = new Vector2(-40f, -218f);

            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.cellSize = new Vector2(164f, 100f);
            layout.spacing = new Vector2(10f, 10f);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    CreateTile(grid, row * Columns + column);
                }
            }

            _previousButton = NewButton("PreviousPage", panel, "<", 116f, 82f, Blue, Cream, () => ChangePage(-1));
            SetBottom(_previousButton.GetComponent<RectTransform>(), 38f, 32f, 116f, 82f, false);

            _nextButton = NewButton("NextPage", panel, ">", 116f, 82f, Blue, Cream, () => ChangePage(1));
            SetBottom(_nextButton.GetComponent<RectTransform>(), 38f, 32f, 116f, 82f, true);

            _pageText = NewText("Page", panel, "", 26f, Navy, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform pageRt = _pageText.rectTransform;
            pageRt.anchorMin = new Vector2(0f, 0f);
            pageRt.anchorMax = new Vector2(1f, 0f);
            pageRt.pivot = new Vector2(0.5f, 0f);
            pageRt.offsetMin = new Vector2(170f, 42f);
            pageRt.offsetMax = new Vector2(-170f, 116f);
        }

        private void CreateTile(Transform parent, int index)
        {
            RectTransform rt = NewRect($"Tile {index + 1}", parent);
            Image background = rt.gameObject.AddComponent<Image>();
            background.raycastTarget = true;
            SetSliced(background);

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI number = NewText("Number", rt, "", 38f, Cream, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform numberRt = number.rectTransform;
            numberRt.anchorMin = Vector2.zero;
            numberRt.anchorMax = Vector2.one;
            numberRt.offsetMin = new Vector2(0f, 22f);
            numberRt.offsetMax = Vector2.zero;

            TextMeshProUGUI state = NewText("State", rt, "", 13f, Cream, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform stateRt = state.rectTransform;
            stateRt.anchorMin = new Vector2(0f, 0f);
            stateRt.anchorMax = new Vector2(1f, 0f);
            stateRt.pivot = new Vector2(0.5f, 0f);
            stateRt.sizeDelta = new Vector2(0f, 24f);
            stateRt.anchoredPosition = new Vector2(0f, 7f);

            Tile tile = new Tile
            {
                Button = button,
                Background = background,
                Number = number,
                State = state
            };
            _tiles.Add(tile);

            button.onClick.AddListener(() => OnTileClicked(tile));
        }

        private void OnTileClicked(Tile tile)
        {
            if (tile == null || tile.Level < 1 || tile.Level > _unlocked) return;

            if (tile.Level == LevelManager.Instance.ActiveLevelNumber)
            {
                Hide();
                return;
            }

            Hide();
            LevelManager.Instance.SelectLevel(tile.Level);
            LevelSelected?.Invoke();
        }

        private void ChangePage(int delta)
        {
            int next = Mathf.Clamp(_page + delta, 0, PageCount - 1);
            if (next == _page) return;
            _page = next;
            Refresh();
        }

        private void Refresh()
        {
            LevelManager manager = LevelManager.Instance;
            _unlocked = Mathf.Clamp(manager.UnlockedLevel, 1, TotalLevels);
            _active = Mathf.Clamp(manager.ActiveLevelNumber, 1, TotalLevels);

            int first = _page * LevelsPerPage + 1;
            int last = Mathf.Min(first + LevelsPerPage - 1, TotalLevels);

            _progressText.text = $"Level {_active}   •   {_unlocked} of {TotalLevels} unlocked";
            _pageText.text = $"{first} - {last}   ({_page + 1} / {PageCount})";
            _previousButton.interactable = _page > 0;
            _nextButton.interactable = _page < PageCount - 1;

            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile tile = _tiles[i];
                int level = first + i;
                tile.Level = level;

                if (level > TotalLevels)
                {
                    tile.Button.gameObject.SetActive(false);
                    continue;
                }

                tile.Button.gameObject.SetActive(true);
                bool unlocked = level <= _unlocked;
                bool current = level == _active;
                bool newest = unlocked && level == _unlocked && !current;

                tile.Button.interactable = unlocked;
                tile.Number.text = level.ToString();

                if (current)
                {
                    tile.Background.color = Gold;
                    tile.Number.color = Navy;
                    tile.State.text = "CURRENT";
                    tile.State.color = Navy;
                }
                else if (unlocked)
                {
                    tile.Background.color = Blue;
                    tile.Number.color = Cream;
                    tile.State.text = newest ? "NEW" : "";
                    tile.State.color = Gold;
                }
                else
                {
                    tile.Background.color = Slate;
                    tile.Number.color = MutedText;
                    tile.State.text = "LOCKED";
                    tile.State.color = MutedText;
                }

                tile.Number.rectTransform.offsetMin = string.IsNullOrEmpty(tile.State.text)
                    ? Vector2.zero
                    : new Vector2(0f, 22f);
            }
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private Image NewImage(string name, Transform parent, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI NewText(string name, Transform parent, string value, float size, Color color,
            TextAlignmentOptions alignment, FontStyles style)
        {
            RectTransform rt = NewRect(name, parent);
            TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private Button NewButton(string name, Transform parent, string label, float width, float height,
            Color background, Color foreground, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rt = NewRect(name, parent);
            rt.sizeDelta = new Vector2(width, height);

            Image image = rt.gameObject.AddComponent<Image>();
            image.color = background;
            image.raycastTarget = true;
            SetSliced(image);

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI text = NewText("Label", rt, label, 38f, foreground, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        private void SetSliced(Image image)
        {
            if (image == null) return;
            image.sprite = _tileSprite;
            image.type = _tileSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static void SetTop(RectTransform rt, float top, float height, float side)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(side, 0f);
            rt.offsetMax = new Vector2(-side, 0f);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            rt.anchoredPosition = new Vector2(0f, -top);
        }

        private static void SetBottom(RectTransform rt, float side, float bottom, float width, float height, bool right)
        {
            rt.anchorMin = new Vector2(right ? 1f : 0f, 0f);
            rt.anchorMax = new Vector2(right ? 1f : 0f, 0f);
            rt.pivot = new Vector2(right ? 1f : 0f, 0f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(right ? -side : side, bottom);
        }
    }
}
