using System;
using UnityEngine;
using SerapKeremGameKit._Audio;
using SerapKeremGameKit._Haptics;

namespace _Game.Line
{
    public class LineAnimation : MonoBehaviour
    {
        private LineRenderer line;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float speedDamp = 8f; // Very fast response to avoid drag
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0, 1f, 1, 1f); // Constant full speed
        [SerializeField] private float minStepDistance = 0.01f; // Minimum distance per frame
        [SerializeField] private float maxMoveDistance = 1.0f; // Maximum distance per frame
        [SerializeField] private string _movementSoundKey = "";

        private bool _isPlaying;
        private bool _forward;
        private Vector3 _direction;
        private Vector3[] positionsOrigin;
        private bool _isInitialized;
        private Vector3[] _tempPositionsArray;
        private Vector3ArrayPool _arrayPool;
        private float _visualZOffset;
        private float _currentSpeed;
        private float _lastUpdateTime;
        private float _accumulatedDeltaTime;

        public bool IsPlaying => _isPlaying;
        public bool IsForward => _forward;
        public Vector3 Direction => _direction;
        public float VisualZOffset
        {
            get => _visualZOffset;
            set
            {
                _visualZOffset = value;
                ApplyVisualZOffset();
            }
        }

        public event Action<bool> OnAnimationStarted;
        public event Action OnAnimationStopped;
        public event Action OnAnimationCompleted;
        public event Action OnLinePositionsChanged;

        public void Initialize(LineRenderer lineRenderer, Vector3ArrayPool arrayPool = null)
        {
            if (lineRenderer == null) return;

            line = lineRenderer;

            var count = line.positionCount;
            if (count < 2) return;

            positionsOrigin = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                positionsOrigin[i] = line.GetPosition(i);
            }

            var lastPoint = line.GetPosition(count - 1);
            _direction = lastPoint - line.GetPosition(count - 2);

            _arrayPool = arrayPool;

            _isInitialized = true;
            line.sortingOrder = 0;
            enabled = false;
        }

        public void Play(bool forwardDirection)
        {
            if (!_isInitialized || line == null || line.positionCount < 2)
                return;

            bool wasPlaying = _isPlaying;
            _forward = forwardDirection;
            _isPlaying = true;
            enabled = true;
            // Draw the active line above idle line bodies. Without this a
            // blocked arrow can disappear under the line it touches before
            // the trigger reverses it, which reads as a flicker.
            line.sortingOrder = 10;

            if (!wasPlaying)
            {
                OnAnimationStarted?.Invoke(forwardDirection);

                if (AudioManager.IsInitialized && !string.IsNullOrEmpty(_movementSoundKey))
                    AudioManager.Instance.Play(_movementSoundKey);
                // Reduced: only play light haptic once at animation start
                if (HapticManager.IsInitialized)
                    HapticManager.Instance.Play(HapticType.Light);
            }
        }


        public void Stop()
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                enabled = false;
                if (line != null) line.sortingOrder = 0;
                OnAnimationStopped?.Invoke();
            }

            if (_tempPositionsArray != null && _arrayPool != null)
            {
                _arrayPool.RecycleArray(_tempPositionsArray);
                _tempPositionsArray = null;
            }
        }

        private void OnDestroy()
        {
            if (_tempPositionsArray != null && _arrayPool != null)
            {
                _arrayPool.RecycleArray(_tempPositionsArray);
                _tempPositionsArray = null;
            }
        }

        private void Update()
        {
            if (!line || line.positionCount < 2)
            {
                _isPlaying = false;
                enabled = false;
                if (line != null) line.sortingOrder = 0;
                OnAnimationStopped?.Invoke();
                return;
            }

            if (_forward)
                AnimateForward();
            else
                AnimateBackward();

            ApplyVisualZOffset();
        }

        private void ApplyVisualZOffset()
        {
            if (!_isInitialized || line == null || line.positionCount == 0) return;

            // Always write the current offset. Besides resetting z back to 0
            // after a reversed line finishes, this catches a newly restored
            // tail point while the line is still at the raised visual depth.
            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 pos = line.GetPosition(i);
                pos.z = _visualZOffset;
                line.SetPosition(i, pos);
            }
        }

        private void AnimateForward()
        {
            var count = line.positionCount;
            var lastPoint = line.GetPosition(count - 1);

            // Direct speed - no drag, no choppy
            float stepDistance = speed * Time.deltaTime * 1.5f;
            lastPoint += _direction.normalized * stepDistance;
            line.SetPosition(count - 1, lastPoint);

            var tailPoint = line.GetPosition(0);
            var tailDirection = line.GetPosition(1) - tailPoint;
            
            // Apply same smoothing to tail movement
            Vector3 tailMoveDistance = tailDirection.normalized * (speed * Time.deltaTime * 1.5f);
            tailPoint += tailMoveDistance;
            line.SetPosition(0, tailPoint);

            OnLinePositionsChanged?.Invoke();

            if (!(Vector2.Distance(tailPoint, line.GetPosition(1)) < 0.1f)) return;

            var newCount = count - 1;
            if (_arrayPool != null)
            {
                _tempPositionsArray = _arrayPool.GetArray(newCount);
            }
            else
            {
                _tempPositionsArray = new Vector3[newCount];
            }

            for (int i = 1; i < count; i++)
            {
                _tempPositionsArray[i - 1] = line.GetPosition(i);
            }

            line.positionCount = newCount;
            line.SetPositions(_tempPositionsArray);

            if (_arrayPool != null)
            {
                _arrayPool.RecycleArray(_tempPositionsArray);
            }
            _tempPositionsArray = null;

            OnLinePositionsChanged?.Invoke();

            if (newCount < 2)
            {
                _isPlaying = false;
                enabled = false;
                line.sortingOrder = 0;
                OnAnimationCompleted?.Invoke();
            }
        }

        private void AnimateBackward()
        {
            int lastIndex = line.positionCount - 1;
            Vector3 currentHeadPos = line.GetPosition(lastIndex);
            Vector3 headMoveDir = -_direction.normalized;
            Vector3 originHeadPos = positionsOrigin[positionsOrigin.Length - 1];

            float distToOrigin = Vector2.Distance(currentHeadPos, originHeadPos);

            // Direct speed - no drag
            float moveDist = speed * Time.deltaTime * 1.5f;

            Vector3 newHeadPos;
            if (distToOrigin > 0.001f)
            {
                Vector3 targetPos = originHeadPos;
                targetPos.z = currentHeadPos.z;
                newHeadPos = Vector3.MoveTowards(currentHeadPos, targetPos, moveDist);
                line.SetPosition(lastIndex, newHeadPos);
            }
            else
            {
                newHeadPos = originHeadPos;
                line.SetPosition(lastIndex, originHeadPos);
            }

            int countCurrent = line.positionCount;
            int countOrigin = positionsOrigin.Length;
            int targetIndex = countOrigin - countCurrent;

            if (targetIndex >= 0)
            {
                Vector3 currentTailPos = line.GetPosition(0);
                Vector3 targetTailPos = positionsOrigin[targetIndex];

                if (Vector2.Distance(currentTailPos, targetTailPos) > 0.1f)
                {
                    Vector3 newTailPos = Vector3.MoveTowards(currentTailPos, targetTailPos, moveDist);

                    newTailPos.z = currentTailPos.z;
                    line.SetPosition(0, newTailPos);
                }
                else
                {
                    targetTailPos.z = currentTailPos.z;
                    line.SetPosition(0, targetTailPos);

                    if (targetIndex > 0)
                    {
                        int newCount = countCurrent + 1;
                        if (_arrayPool != null)
                            _tempPositionsArray = _arrayPool.GetArray(newCount);
                        else
                            _tempPositionsArray = new Vector3[newCount];

                        _tempPositionsArray[0] = targetTailPos;

                        for (int i = 0; i < countCurrent; i++)
                        {
                            _tempPositionsArray[i + 1] = line.GetPosition(i);
                        }

                        line.positionCount = newCount;
                        line.SetPositions(_tempPositionsArray);

                        if (_arrayPool != null)
                        {
                            _arrayPool.RecycleArray(_tempPositionsArray);
                            _tempPositionsArray = null;
                        }
                    }
                    else
                    {
                        if (Vector2.Distance(newHeadPos, originHeadPos) < 0.01f)
                        {
                            _isPlaying = false;
                            enabled = false;
                            line.sortingOrder = 0;
                            OnAnimationCompleted?.Invoke();
                        }
                    }
                }
                OnLinePositionsChanged?.Invoke();
            }
        }
    }
}