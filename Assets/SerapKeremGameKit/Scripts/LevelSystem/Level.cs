using SerapKeremGameKit._Camera;
using SerapKeremGameKit._InputSystem;
using SerapKeremGameKit._Logging;
using SerapKeremGameKit._Managers;
using SerapKeremGameKit._UI;
using System.Collections;
using TriInspector;
using UnityEngine;
using Array2DEditor;
using _Game.Line;
using _Game.UI;
using _Game.Ads;



namespace SerapKeremGameKit._LevelSystem
{
    public class Level : MonoBehaviour
    {

        [Title("Grid Settings"), PropertyOrder(2)]
        [SerializeField] private Array2DInt _tileSizeArray;

        [Title("Time Settings")]
        [SerializeField, Min(0f)] private float _levelTime = 120f;
        public float LevelTime => _levelTime;

        [ReadOnly]
        [SerializeField] private bool _isLevelWon;

        [Title("Money Settings")]
        [SerializeField] private long _money = 10;
        public long Money => _money;


        private Coroutine _winCoroutine;
        private Coroutine _loseCoroutine;

        [SerializeField] private LineManager _lineManager;
        public LineManager LineManager { get => _lineManager; set => _lineManager = value; }

        [SerializeField] private Transform _linesParent;
        public virtual void Load()
        {
            gameObject.SetActive(true);
            _isLevelWon = false;
            if (_winCoroutine != null) { StopCoroutine(_winCoroutine); _winCoroutine = null; }
            if (_loseCoroutine != null) { StopCoroutine(_loseCoroutine); _loseCoroutine = null; }
            
            UnsubscribeFromEvents();
            Initialize();
        }

        private void Initialize()
        {
            InitializeCamera();
            InitializeLines();
        }

        private void InitializeLines()
        {
            if (_lineManager != null)
            {
                _lineManager.InitializeLines(transform);
            }
            else
            {
                TraceLogger.LogWarning("LineManager is not initialized. Lines will not be initialized.", this);
            }
        }

        private void InitializeCamera()
        {
            if (CameraManager.Instance == null)
            {
                TraceLogger.LogError("CameraManager.Instance is null! Cannot initialize camera position.", this);
                return;
            }

            if (_linesParent == null)
            {
                TraceLogger.LogWarning("Lines parent is not assigned in Inspector. Camera will not be fitted to lines.", this);
                return;
            }

            CameraManager.Instance.FitCameraToLines(_linesParent);
        }

        public virtual void Play()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.UnlockInput();
            }

            if (AdsManager.Exists && AdsManager.Instance != null)
            {
                AdsManager.Instance.ResetLevelState();
            }

            _isLevelWon = false;
            
            if (_winCoroutine != null) 
            { 
                StopCoroutine(_winCoroutine); 
                _winCoroutine = null; 
            }
            
            if (_loseCoroutine != null) 
            { 
                StopCoroutine(_loseCoroutine); 
                _loseCoroutine = null; 
            }

            UnsubscribeFromEvents();

            SubscribeToLivesManager();

            if (_lineManager != null)
            {
                _lineManager.OnAllLinesRemoved += HandleAllLinesRemoved;
            }

            InitializeHUD();
        }

        private void InitializeHUD()
        {
            UIRootController uiRoot = FindFirstObjectByType<UIRootController>();
            if (uiRoot != null)
            {
                uiRoot.InitializeHUD();
            }
        }

        private void SubscribeToLivesManager()
        {
            if (LivesManager.IsInitialized && LivesManager.Instance != null)
            {
                LivesManager.Instance.Initialize();
                LivesManager.Instance.OnLivesDepleted += HandleLivesDepleted;
                
                if (LivesManager.Instance.CurrentLives <= 0)
                {
                    HandleLivesDepleted();
                }
            }
            else
            {
                StartCoroutine(SubscribeToLivesManagerCoroutine());
            }
        }

        private IEnumerator SubscribeToLivesManagerCoroutine()
        {
            int maxAttempts = 10;
            int attempts = 0;
            
            while (attempts < maxAttempts)
            {
                if (LivesManager.IsInitialized && LivesManager.Instance != null)
                {
                    LivesManager.Instance.Initialize();
                    LivesManager.Instance.OnLivesDepleted += HandleLivesDepleted;
                    
                    if (LivesManager.Instance.CurrentLives <= 0)
                    {
                        HandleLivesDepleted();
                    }
                    
                    yield break;
                }
                
                yield return null;
                attempts++;
            }

            TraceLogger.LogWarning("LivesManager is not initialized after multiple attempts. Fail condition may not work.", this);
        }


        private void HandleLivesDepleted()
        {
            if (_loseCoroutine != null) return;

            // Before conceding, give the player a chance to earn a life back with a rewarded
            // video. If no ad is available this falls straight through to the normal loss.
            if (TryOfferExtraLife()) return;

            CheckLoseCondition();
        }

        /// <summary>
        /// Shows the "+1 life" rewarded prompt. Returns true when the prompt was shown and the
        /// outcome is now the player's to decide.
        /// </summary>
        private bool TryOfferExtraLife()
        {
            if (!AdsManager.Exists || AdsManager.Instance == null) return false;
            if (!AdsManager.Instance.CanOfferExtraLife) return false;
            if (!AdsManager.Instance.RewardedReady) return false;
            if (!LivesManager.IsInitialized || LivesManager.Instance == null) return false;

            AdsManager.Instance.ConsumeExtraLifeOffer();

            if (InputHandler.Instance != null) InputHandler.Instance.LockInput();

            AdsRuntimeUI.ShowExtraLifePrompt(
                onAccept: () =>
                {
                    AdsManager.Instance.ShowRewarded(
                        onRewarded: () =>
                        {
                            LivesManager.Instance.GrantLife(1);
                            if (InputHandler.Instance != null) InputHandler.Instance.UnlockInput();
                        },
                        onUnavailable: () =>
                        {
                            // Ad vanished between the check and the tap — resume the loss.
                            if (InputHandler.Instance != null) InputHandler.Instance.UnlockInput();
                            CheckLoseCondition();
                        });
                },
                onDecline: () =>
                {
                    CheckLoseCondition();
                });

            return true;
        }

        private void HandleAllLinesRemoved()
        {
            CheckWinCondition();
        }

        public void CheckWinCondition()
        {
            if (_isLevelWon) return;

            _isLevelWon = true;
            _winCoroutine = StartCoroutine(WinCoroutine());
        }

        private IEnumerator WinCoroutine()
        {
            if (InputHandler.Instance != null) InputHandler.Instance.LockInput();
            yield return new WaitForSeconds(0.5f);
            LevelManager.Instance.Win();
        }

        public void CheckLoseCondition()
        {
            if (_loseCoroutine != null) return;

            _loseCoroutine = StartCoroutine(LoseCoroutine());
        }

        private IEnumerator LoseCoroutine()
        {
            if (InputHandler.Instance != null) InputHandler.Instance.LockInput();
            yield return new WaitForSeconds(0.5f);

            LevelManager.Instance.Lose();
        }

        private void UnsubscribeFromEvents()
        {
            if (LivesManager.IsInitialized && LivesManager.Instance != null)
            {
                LivesManager.Instance.OnLivesDepleted -= HandleLivesDepleted;
            }

            if (_lineManager != null)
            {
                _lineManager.OnAllLinesRemoved -= HandleAllLinesRemoved;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}