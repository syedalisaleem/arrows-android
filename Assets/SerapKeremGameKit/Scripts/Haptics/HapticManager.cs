using SerapKeremGameKit._Singletons;
using SerapKeremGameKit._Utilities;
using UnityEngine;

namespace SerapKeremGameKit._Haptics
{
    public sealed class HapticManager : MonoSingleton<HapticManager>
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField, Range(0f, 1f)] private float _globalIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float _cooldownSeconds = 0.1f;

        [Header("Per-Type Intensity")]
        [SerializeField, Range(0f, 1f)] private float _lightIntensity = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _mediumIntensity = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _heavyIntensity = 1f;

        private float _nextAllowedTime;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            _enabled = PlayerPrefs.GetInt(PreferencesKeys.SettingsHaptic, 1) == 1;
        }

        public void SetEnabled(bool isEnabled)
        {
            _enabled = isEnabled;
            PlayerPrefs.SetInt(PreferencesKeys.SettingsHaptic, _enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetGlobalIntensity(float intensity)
        {
            _globalIntensity = Mathf.Clamp01(intensity);
        }

        public void Play(HapticType type)
        {
            if (!_enabled) return;
            if (Time.unscaledTime < _nextAllowedTime) return;

            float intensity = GetIntensityForType(type) * _globalIntensity;
            if (intensity <= 0f) return;

            _nextAllowedTime = Time.unscaledTime + _cooldownSeconds;

#if UNITY_ANDROID || UNITY_IOS
            PlayMobile(type, intensity);
#else
            // Editor/Unsupported: no-op (or optional log)
#endif
        }

        private static float GetIntensityForType(HapticType type)
        {
            return type switch
            {
                HapticType.None => 0f,
                HapticType.Selection => 0.25f,
                HapticType.Light => 0.3f,
                HapticType.Medium => 0.6f,
                HapticType.Heavy => 1f,
                HapticType.Success => 0.8f,
                HapticType.Warning => 0.8f,
                HapticType.Failure => 1f,
                _ => 0.5f,
            };
        }

#if UNITY_ANDROID || UNITY_IOS
        private static void PlayMobile(HapticType type, float intensity)
        {
            // Trigger vibration at configured intensity level
            Handheld.Vibrate();

            // TODO: For full per-type intensity control on Android,
            // integrate with platform-specific vibration APIs (e.g.
            // VibrationEffect APIs for newer Android versions).
        }
#endif
    }
}


