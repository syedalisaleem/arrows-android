using UnityEngine;
using UnityEngine.EventSystems;

namespace SerapKeremGameKit._Camera
{
    /// <summary>
    /// Lightweight additive zoom controller for the orthographic gameplay camera.
    /// It never changes the fit chosen by CameraManager on level load; it only
    /// lets the player pinch (or use the mouse wheel in the Editor) afterwards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraZoomController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _maxZoomIn = 0.5f;
        [SerializeField, Min(1f)] private float _maxZoomOut = 1.2f;
        [SerializeField, Min(0.01f)] private float _wheelStep = 0.9f;

        public static bool IsPinching { get; private set; }

        private Camera _camera;
        private float _fitSize;
        private Vector3 _fitPosition;
        private bool _pinching;
        private float _lastPinchDistance;
        private Vector2 _lastPinchMidpoint;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void ResetView(float orthographicSize, Vector3 position)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null || !_camera.orthographic) return;

            _fitSize = Mathf.Max(0.1f, orthographicSize);
            _fitPosition = position;
            _camera.orthographicSize = _fitSize;
            _camera.transform.position = _fitPosition;

            _pinching = false;
            IsPinching = false;
        }

        private void Update()
        {
            if (_camera == null || !_camera.orthographic) return;

            if (Time.timeScale <= 0f)
            {
                _pinching = false;
                IsPinching = false;
                return;
            }

            HandlePinch();
            HandleWheel();
        }

        private void HandlePinch()
        {
            if (Input.touchCount < 2)
            {
                _pinching = false;
                IsPinching = false;
                return;
            }

            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            Vector2 midpoint = (first.position + second.position) * 0.5f;
            float distance = Vector2.Distance(first.position, second.position);

            if (!_pinching || first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
            {
                _pinching = true;
                IsPinching = true;
                _lastPinchDistance = distance;
                _lastPinchMidpoint = midpoint;
                return;
            }

            if (_lastPinchDistance > 1f && distance > 1f)
            {
                float ratio = _lastPinchDistance / distance;
                ApplyZoom(_camera.orthographicSize * ratio, midpoint);
            }

            Vector2 fingerDelta = midpoint - _lastPinchMidpoint;
            if (fingerDelta.sqrMagnitude > 0.01f)
            {
                float worldPerPixel = (2f * _camera.orthographicSize) / Mathf.Max(1, Screen.height);
                _camera.transform.position -= new Vector3(fingerDelta.x, fingerDelta.y, 0f) * worldPerPixel;
                ClampPosition();
            }

            _lastPinchDistance = distance;
            _lastPinchMidpoint = midpoint;
        }

        private void HandleWheel()
        {
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f) return;

            // Do not zoom the board while the pointer is over the HUD or an
            // overlay. Touch devices do not produce wheel input, so this is
            // mainly an Editor/desktop safeguard.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            float targetSize = _camera.orthographicSize * Mathf.Pow(_wheelStep, wheel);
            ApplyZoom(targetSize, Input.mousePosition);
        }

        private void ApplyZoom(float targetSize, Vector2 screenFocus)
        {
            if (_fitSize <= 0f) return;

            float minSize = _fitSize * _maxZoomIn;
            float maxSize = _fitSize * _maxZoomOut;
            targetSize = Mathf.Clamp(targetSize, minSize, maxSize);

            if (Mathf.Abs(targetSize - _camera.orthographicSize) < 0.0005f) return;

            Vector3 worldBefore = ScreenToWorld(screenFocus);
            _camera.orthographicSize = targetSize;
            Vector3 worldAfter = ScreenToWorld(screenFocus);
            _camera.transform.position += worldBefore - worldAfter;

            ClampPosition();
        }

        private Vector3 ScreenToWorld(Vector2 screenPoint)
        {
            float distance = Mathf.Abs(_camera.transform.position.z);
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, distance));
            world.z = _camera.transform.position.z;
            return world;
        }

        private void ClampPosition()
        {
            if (_fitSize <= 0f) return;

            float size = _camera.orthographicSize;
            float maxX = Mathf.Max(0f, (_fitSize - size) * _camera.aspect);
            float maxY = Mathf.Max(0f, _fitSize - size);

            Vector3 offset = _camera.transform.position - _fitPosition;
            if (size > _fitSize)
            {
                offset.x = 0f;
                offset.y = 0f;
            }
            else
            {
                offset.x = Mathf.Clamp(offset.x, -maxX, maxX);
                offset.y = Mathf.Clamp(offset.y, -maxY, maxY);
            }

            offset.z = 0f;
            _camera.transform.position = _fitPosition + offset;
        }

        private void OnDisable()
        {
            _pinching = false;
            IsPinching = false;
        }
    }
}
