using System;
using UnityEngine;
using SerapKeremGameKit._Singletons;
using SerapKeremGameKit._Managers;

namespace _Game.UI
{
    public class LivesManager : MonoSingleton<LivesManager>
    {
        [Header("Lives Settings")]
        [SerializeField] private int _maxLives = 5;
        
        private int _currentLives;
        private int _lastLifeLossFrame = -1;

        public int CurrentLives => _currentLives;
        public int MaxLivesCount => _maxLives;

        public event Action<int> OnLivesChanged;
        public event Action OnLivesDepleted;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize()
        {
            ResetLives();
        }

        public void ResetLives()
        {
            _currentLives = _maxLives;
            _lastLifeLossFrame = -1;
            OnLivesChanged?.Invoke(_currentLives);
        }

        public void LoseLife()
        {
            int currentFrame = Time.frameCount;
            
            if (_lastLifeLossFrame == currentFrame)
            {
                return;
            }

            if (_currentLives <= 0)
            {
                return;
            }

            _lastLifeLossFrame = currentFrame;
            _currentLives--;
            OnLivesChanged?.Invoke(_currentLives);

            if (_currentLives <= 0)
            {
                OnLivesDepleted?.Invoke();
            }
        }

        public bool HasLivesRemaining()
        {
            return _currentLives > 0;
        }

        /// <summary>
        /// Adds lives back after a rewarded ad. Clears the frame guard so the very next
        /// collision is allowed to cost a life again.
        /// </summary>
        public void GrantLife(int amount = 1)
        {
            if (amount <= 0) return;

            _currentLives = Mathf.Min(_currentLives + amount, _maxLives);
            _lastLifeLossFrame = -1;
            OnLivesChanged?.Invoke(_currentLives);
        }
    }
}
