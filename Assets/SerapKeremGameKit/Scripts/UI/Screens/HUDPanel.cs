using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Game.UI;

namespace SerapKeremGameKit._UI
{
    public sealed class HUDPanel : UIPanel
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private UIRootController _uiRoot;
        [SerializeField] private HeartPanel _heartPanel;

        private LevelSelectMenu _levelMenu;
        private Button _levelsButton;
        private bool _isInitialized = false;

        private static readonly Color LevelButtonBlue = new Color(0.196f, 0.545f, 0.953f, 1f);
        private static readonly Color LevelButtonCream = new Color(0.969f, 0.933f, 0.855f, 1f);

        private void Awake()
        {
            if (_restartButton != null) _restartButton.BindOnClick(this, OnRestartClicked);
            if (_settingsButton != null) _settingsButton.BindOnClick(this, OnSettingsClicked);
            EnsureLevelsButton();
        }

        public override void Show(bool playSound = true)
        {
            base.Show(playSound);
            
            if (!_isInitialized)
            {
                Initialize();
            }
            
            SubscribeToLivesManager();
            InitializeHeartPanel();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromLivesManager();
            // Auto-unsubscribe handled by ButtonExtensions
        }

        private void SubscribeToLivesManager()
        {
            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
            {
                _uiRoot.LivesManagerInstance.OnLivesChanged += HandleLivesChanged;
            }
        }

        private void UnsubscribeFromLivesManager()
        {
            if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
            {
                _uiRoot.LivesManagerInstance.OnLivesChanged -= HandleLivesChanged;
            }
        }

        private void InitializeHeartPanel()
        {
            if (_heartPanel != null)
            {
                _heartPanel.Initialize();
                if (_uiRoot != null && _uiRoot.LivesManagerInstance != null)
                {
                    _heartPanel.UpdateHearts(_uiRoot.LivesManagerInstance.CurrentLives);
                }
            }
        }

        private void HandleLivesChanged(int currentLives)
        {
            if (_heartPanel != null)
            {
                _heartPanel.UpdateHearts(currentLives);
            }
        }

        public void SetLevelIndex(int levelIndex)
        {
            if (_levelText != null)
                _levelText.text = $"Level {levelIndex + 1}";
        }

        public void UpdateTimeDisplay(float remainingTime)
        {
            if (_timeText != null)
            {
                _timeText.text = FormatTime(remainingTime);
            }
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;

            return $"{minutes:D2}:{remainingSeconds:D2}";
        }

        private void OnRestartClicked()
        {
            if (_uiRoot != null) _uiRoot.OnRestartRequested();
        }

        private void EnsureLevelsButton()
        {
            if (_levelsButton != null) return;

            Transform parent = _settingsButton != null ? _settingsButton.transform.parent : transform;
            GameObject go = new GameObject("LevelsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(54f, -274f);
            rt.sizeDelta = new Vector2(196f, 64f);

            Image background = go.GetComponent<Image>();
            background.color = LevelButtonBlue;
            background.raycastTarget = true;
            Sprite themeSprite = ThemeSprite;
            if (themeSprite != null)
            {
                background.sprite = themeSprite;
                background.type = Image.Type.Sliced;
            }

            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -4f);

            _levelsButton = go.GetComponent<Button>();
            _levelsButton.targetGraphic = background;
            _levelsButton.navigation = new Navigation { mode = Navigation.Mode.None };
            _levelsButton.onClick.AddListener(OnLevelsClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(go.transform, false);
            RectTransform labelRt = labelObject.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = FontAsset;
            label.text = "LEVELS";
            label.fontSize = 27f;
            label.fontStyle = FontStyles.Bold;
            label.color = LevelButtonCream;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private void OnLevelsClicked()
        {
            if (_levelMenu == null)
            {
                Transform canvasRoot = CanvasRoot;
                if (canvasRoot == null) return;
                _levelMenu = LevelSelectMenu.Create(canvasRoot, FontAsset, ThemeSprite);
            }

            _levelMenu.Show();
        }

        public TMP_FontAsset FontAsset => _levelText != null ? _levelText.font : TMP_Settings.defaultFontAsset;

        public Sprite ThemeSprite
        {
            get
            {
                if (_levelText != null)
                {
                    Image badge = _levelText.GetComponentInParent<Image>();
                    if (badge != null && badge.sprite != null) return badge.sprite;
                }

                if (_settingsButton != null)
                {
                    Image icon = _settingsButton.GetComponentInChildren<Image>(true);
                    if (icon != null) return icon.sprite;
                }

                return null;
            }
        }

        public Transform CanvasRoot
        {
            get
            {
                if (_levelText != null && _levelText.canvas != null) return _levelText.canvas.transform;
                return transform.root;
            }
        }

        private void OnSettingsClicked()
        {
            if (_uiRoot != null) _uiRoot.OnOpenSettings();
        }

        public void SetUIRoot(UIRootController uiRoot)
        {
            _uiRoot = uiRoot;
        }
    }
}