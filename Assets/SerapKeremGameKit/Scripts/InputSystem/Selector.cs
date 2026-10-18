using System;
using TriInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using SerapKeremGameKit._Logging;
using SerapKeremGameKit._InputSystem.Data;
using SerapKeremGameKit._Camera;

namespace SerapKeremGameKit._InputSystem
{
    public class Selector : MonoBehaviour
    {
        [Title("Selector Settings")]
        [SerializeField] private PlayerInputSO _playerInputSO;

        [Header("Raycasting")]
        [SerializeField] private LayerMask _selectableLayerMash;
        [SerializeField, Range(10f, 1000f)] private float _raycastDistance = 500f;
        [SerializeField] private bool _use2DColliders = true;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugRay = true;

        [Header("Camera")]
        [SerializeField, ReadOnly] private Camera _mainCamera;

        //[Header("Audio")]
        // Integrate with your AudioManager if desired later

        private void Awake()
        {
            _mainCamera = Camera.main;
            ValidateReferences();
        }

        private void Update()
        {
            if (_playerInputSO == null) return;

            // Paused/menu input and multi-touch gestures must never fall
            // through and start a line by accident.
            if (Time.timeScale <= 0f) return;
            if (Input.touchCount > 1 || CameraZoomController.IsPinching) return;

            if (_playerInputSO.DownThisFrame) HandleSelectStart(_playerInputSO.MousePosition);
            else if (_playerInputSO.Held) HandleDrag(_playerInputSO.MousePosition);
            else if (_playerInputSO.UpThisFrame) HandleRelease(_playerInputSO.MousePosition);
        }

        private void ValidateReferences()
        {
            if (_mainCamera == null)
                TraceLogger.LogError("Main Camera not found. Make sure it is tagged 'MainCamera'.");

            if (_playerInputSO == null)
                TraceLogger.LogError("PlayerInputSO reference is missing.");
        }

        private void HandleSelectStart(Vector3 screenPos)
        {
            if (IsPointerOverUI()) return;

            if (_use2DColliders)
            {
                Handle2DSelection(screenPos);
            }
            else
            {
                Handle3DSelection(screenPos);
            }
        }

        private void Handle2DSelection(Vector3 screenPos)
        {
            if (_mainCamera == null) return;

            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(screenPos);
            mouseWorldPos.z = 0f;

            // Physics2D.OverlapPoint makes the player hit the exact 0.2-unit
            // stroke, which feels unresponsive on a phone. Use a screen-space
            // tolerance (scaled by zoom) and pick the nearest line instead.
            float worldPerPixel = 2f * GetWorldHeight() / Mathf.Max(1, Screen.height);
            float radius = Mathf.Clamp(worldPerPixel * 32f, 0.08f, 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPos, radius, _selectableLayerMash);
            ISelectable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null) continue;

                ISelectable selectable = hit.GetComponent<ISelectable>();
                if (selectable == null)
                {
                    selectable = hit.GetComponentInParent<ISelectable>();
                }
                if (selectable == null) continue;

                Vector2 closest = hit.ClosestPoint(mouseWorldPos);
                float distance = (closest - (Vector2)mouseWorldPos).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = selectable;
                }
            }

            if (best != null)
            {
                ProcessSelection(best, mouseWorldPos);
                DrawDebugRay2D(mouseWorldPos, Color.green);
            }
            else
            {
                DrawDebugRay2D(mouseWorldPos, Color.red);
            }
        }

        private float GetWorldHeight()
        {
            if (_mainCamera.orthographic)
            {
                return _mainCamera.orthographicSize * 2f;
            }

            return Mathf.Max(1f, _raycastDistance * 0.25f);
        }

        private void Handle3DSelection(Vector3 screenPos)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out var hit, _raycastDistance, _selectableLayerMash))
            {
                if (hit.collider)
                {
                    GameObject selectedObject = hit.collider.gameObject;
                    
                    ISelectable selectable = selectedObject.GetComponent<ISelectable>();
                    if (selectable == null)
                    {
                        selectable = selectedObject.GetComponentInParent<ISelectable>();
                    }
                    
                    if (selectable != null)
                    {
                        ProcessSelection(selectable, hit.point);
                        DrawDebugRay(ray, hit.distance, Color.green);
                    }
                    else
                    {
                        DrawDebugRay(ray, hit.distance, Color.yellow);
                    }
                }
            }
            else
            {
                DrawDebugRay(ray, _raycastDistance, Color.red);
            }
        }

        private void ProcessSelection(ISelectable selectable, Vector3 worldPosition)
        {
            if (selectable != null)
            {
                selectable.OnSelected(worldPosition);
            }
        }

        private void DrawDebugRay2D(Vector3 worldPos, Color color)
        {
            if (_enableDebugRay)
            {
                Debug.DrawRay(worldPos, Vector3.up * 0.1f, color, 1f);
            }
        }

        private void HandleDrag(Vector3 screenPos)
        {
            //if (!IsGamePlayable()) return;
            if (IsPointerOverUI()) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out var hit, _raycastDistance, _selectableLayerMash))
            {
                if (hit.collider)
                {
                 
                }

                DrawDebugRay(ray, hit.distance, Color.green);
            }
            else
            {
                DrawDebugRay(ray, _raycastDistance, Color.red);
            }
        }

        private void HandleRelease(Vector3 screenPos)
        {
           
        }

        //private bool IsGamePlayable() => StateManager.Instance.CurrentState == GameState.OnStart;

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            // On mobile, IsPointerOverGameObject() checks only the mouse
            // pointer. Check every active finger so taps that land on the HUD
            // cannot pass through and select a line underneath it.
            int touchCount = Input.touchCount;
            for (int i = 0; i < touchCount; i++)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                {
                    return true;
                }
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        private void DrawDebugRay(Ray ray, float distance, Color color)
        {
            if (_enableDebugRay)
                Debug.DrawRay(ray.origin, ray.direction * distance, color, 1f);
        }
    }
}