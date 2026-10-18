using SerapKeremGameKit._LevelSystem;
using SerapKeremGameKit._Singletons;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;
using SerapKeremGameKit._Logging;
using SerapKeremGameKit._Utilities;

namespace SerapKeremGameKit._Managers
{
    [DefaultExecutionOrder(-2)]
    public class LevelManager : MonoSingleton<LevelManager>
    {
        #region Properties & Data Access

        private const string ProgressKey = PreferencesKeys.ProgressData;
        private const string UnlockedLevelKey = PreferencesKeys.UnlockedLevel;

        public int ActiveLevelNumber
        {
            get => PlayerPrefs.GetInt(ProgressKey, 1);
            set { PlayerPrefs.SetInt(ProgressKey, value); SaveUtility.SaveImmediate(); }
        }

        /// <summary>
        /// Highest level the player has earned access to. ActiveLevelNumber is
        /// the level currently being played; selecting an earlier level must
        /// never relock the levels that were already reached.
        /// </summary>
        public int UnlockedLevel
        {
            get
            {
                int stored = PlayerPrefs.GetInt(UnlockedLevelKey, ActiveLevelNumber);
                return Mathf.Clamp(Mathf.Max(Mathf.Max(stored, ActiveLevelNumber), 1), 1, TotalLevelCount);
            }
            set
            {
                int target = Mathf.Max(UnlockedLevel, Mathf.Clamp(value, 1, TotalLevelCount));
                PlayerPrefs.SetInt(UnlockedLevelKey, target);
                SaveUtility.SaveImmediate();
            }
        }

        [Title("Level Collections")]
        [ListDrawerSettings(Draggable = true, AlwaysExpanded = false)]
        [FormerlySerializedAs("_gameplayLevels")]
        [SerializeField, Required] private Level[] _levels;

        [Title("Procedural Levels")]
        [Tooltip("Total number of levels. Anything past the handcrafted array is built at runtime from Resources/Levels/procedural_levels.txt.")]
        [SerializeField, Min(1)] private int _totalLevelCount = 500;

        public Level ActiveLevelInstance { get; private set; }
        public int ProcessedLevelIndex { get; private set; }

        // Public accessors for external systems
        public Level[] GameplayLevels => _levels;
        public int GameplayLevelCount => _levels != null ? _levels.Length : 0;
        public int TotalLevelCount => Mathf.Max(_totalLevelCount, _levels != null ? _levels.Length : 0);

        #endregion

        // Events removed to keep template simple and robust

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            PerformInitialValidation();
        }

        void Start()
        {
            StartCurrentLevelInstance();
        }

        public void StartCurrentLevelInstance()
        {
            ConfigureEnvironment();
            LoadCurrentLevel();
        }

        #endregion

        #region Core Level Management

        public void LoadCurrentLevel()
        {
            var selection = ComputeLevelSelection();
            ProcessedLevelIndex = selection.targetIndex;
            InstantiateAndBegin(selection.selectedLevel, selection.targetIndex);
        }

        private (Level selectedLevel, int targetIndex) ComputeLevelSelection()
        {
            return ResolveGameplaySelection(ActiveLevelNumber);
        }

        private (Level selectedLevel, int targetIndex) ResolveGameplaySelection(int progress)
        {
            int index = Mathf.Clamp(progress, 1, TotalLevelCount);

            if (_levels != null && index <= _levels.Length)
                return (_levels[index - 1], index);

            Level proceduralBase = Resources.Load<Level>("Levels/Level_Base");
            if (proceduralBase == null)
            {
                TraceLogger.LogError("Procedural base level is missing at Resources/Levels/Level_Base. Falling back to level 1.", this);
                return (_levels != null && _levels.Length > 0 ? _levels[0] : null, 1);
            }

            return (proceduralBase, index);
        }

        private void InstantiateAndBegin(Level targetLevel, int levelNumber)
        {
            ActiveLevelInstance = Instantiate(targetLevel);
            ActiveLevelInstance.name = $"Level {levelNumber}";

            if (_levels == null || levelNumber > _levels.Length)
            {
                _Game.Levels.ProceduralLevelBuilder.Build(ActiveLevelInstance, levelNumber);
            }

            ActiveLevelInstance.Load();
            Time.timeScale = 1f;
            if (SerapKeremGameKit._InputSystem.InputHandler.Instance != null)
            {
                SerapKeremGameKit._InputSystem.InputHandler.Instance.UnlockInput();
            }
            StateManager.Instance.SetLoading();
            // UI: update level text on load
            //UIManager.Instance?.RefreshLevelNumber();
            StartLevel();
        }

        #endregion

        #region Level Control Methods

        public void StartLevel()
        {
            ActiveLevelInstance.Play();
            StateManager.Instance.SetOnStart();
            // UI: show in-game UI and refresh text
            //UIManager.Instance?.ShowInGameUI();
            //UIManager.Instance?.RefreshLevelNumber();
        }

        public void RetryLevel()
        {
            TerminateCurrentLevel();
            LoadCurrentLevel();
        }

        public void RestartLevel()
        {
            StateManager.Instance.SetOnRestart();
            RetryLevel();
        }

        public void CleanCurrentLevel()
        {
            TerminateCurrentLevel();
            // UI: hide gameplay UI if needed
            //UIManager.Instance?.HideInGameUI();
        }

        public void IncreaseLevelNumber()
        {
            TerminateCurrentLevel();
            int next = Mathf.Min(ActiveLevelNumber + 1, TotalLevelCount);
            ActiveLevelNumber = next;
            UnlockedLevel = next;
        }

        /// <summary>
        /// Loads a level chosen from the level-select menu. Locked levels are
        /// rejected here as well as in the UI so a stale/duplicated button can
        /// never bypass the progression rule.
        /// </summary>
        public void SelectLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > TotalLevelCount) return;
            if (levelNumber > UnlockedLevel) return;

            TerminateCurrentLevel();
            ActiveLevelNumber = levelNumber;
            LoadCurrentLevel();
        }

        private void TerminateCurrentLevel()
        {
            // notify state if needed
            if (ActiveLevelInstance != null)
                Destroy(ActiveLevelInstance.gameObject);
        }

        #endregion

        #region Game Result Handlers
        [Button("Test LevelWin")]
        public void Win()
        {
            if (!ValidateGameStateForEvents()) return;
            StateManager.Instance.SetOnWin();
            // Example: level-based coin reward
            // Currency.Currency.Add(ActiveLevelNumber * 5);
            // UI: show win panel
            //UIManager.Instance?.ShowWinScreen();
        }

        [Button("Test LevelWin")]
        public void Win(int moveCount)
        {
            if (!ValidateGameStateForEvents()) return;
            StateManager.Instance.SetOnWin();
            // Example: move-based bonus
            // int bonus = Mathf.Max(0, 10 - moveCount);
            // Currency.Currency.Add(bonus);
        }

        [Button("Test LevelLose")]
        public void Lose()
        {
            if (!ValidateGameStateForEvents()) return;
            StateManager.Instance.SetOnLose();
            // UI: show fail panel
            //UIManager.Instance?.ShowFailScreen();
        }

        private bool ValidateGameStateForEvents()
        {
            return StateManager.Instance.CurrentState == GameState.OnStart;
        }

        #endregion

        #region Utility & Validation Methods

        private void PerformInitialValidation()
        {
            if (_levels == null || _levels.Length == 0)
                TraceLogger.LogWarning($"{name}: Levels array is not configured.", this);
        }

        private void ConfigureEnvironment()
        {
#if UNITY_EDITOR
            CleanupExistingLevelsInEditor();
#endif
        }

#if UNITY_EDITOR
        private void CleanupExistingLevelsInEditor()
        {
            var existingLevelInstances = FindObjectsByType<Level>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var levelInstance in existingLevelInstances)
                levelInstance.gameObject.SetActive(false);
        }
#endif

        #endregion

        public Level GetLevelByNumber(int levelNumber)
        {
            int gameplayIndex = levelNumber;

            if (gameplayIndex <= 0 || gameplayIndex > _levels.Length) return null;

            return _levels[gameplayIndex - 1];
        }

        #region Utility & Validation Methods
        public Level GetCurrentLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber);
        }

        public Level GetNextLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 1) ?? GetLevelByNumber(1);
        }

        public Level GetNextestLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 2) ?? GetLevelByNumber(1);
        }

        public Level GetFinalLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 3) ?? GetLevelByNumber(1);
        }

        public Level GetFinalNextLevel()
        {
            return GetLevelByNumber(ActiveLevelNumber + 4) ?? GetLevelByNumber(1);
        }
        #endregion
    }
}