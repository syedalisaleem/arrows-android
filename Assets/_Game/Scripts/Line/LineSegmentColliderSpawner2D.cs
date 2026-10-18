using System.Collections.Generic;
using UnityEngine;

namespace _Game.Line
{
    public class LineSegmentColliderSpawner2D : MonoBehaviour
    {
        [Header("References")]
        private LineRenderer lineRenderer;
        [SerializeField] private GameObject segmentPrefab;

        [Header("Collider Settings")]
        [SerializeField] private float thickness = 0.2f;
        [SerializeField] private float extraLenght = 0.2f;
        [SerializeField] private bool autoUpdateInPlayMode = true;

        private readonly List<GameObject> _spawnedSegments = new();

        public void Initialize(LineRenderer lineRenderer)
        {
            this.lineRenderer = lineRenderer;

            if (autoUpdateInPlayMode && Application.isPlaying)
            {
                RebuildSegments();
            }
        }

        [ContextMenu("Rebuild Segment Colliders")]
        private void RebuildSegmentsContextMenu()
        {
            RebuildSegments();
        }

        private void RebuildSegments()
        {
            if (!lineRenderer || !segmentPrefab)
                return;

            ClearSegments();

            int count = lineRenderer.positionCount;
            if (count < 2)
                return;

            for (int i = 0; i < count - 1; i++)
            {
                GameObject instance = CreateSegment();
                _spawnedSegments.Add(instance);
                UpdateSegment(instance, i);
            }
        }

        public void UpdateSegments()
        {
            if (!lineRenderer || !segmentPrefab)
                return;

            int count = lineRenderer.positionCount;
            if (count < 2)
            {
                ClearSegments();
                return;
            }

            int segmentCount = count - 1;
            int currentCount = _spawnedSegments.Count;

            if (currentCount == 0 && segmentCount > 0)
            {
                RebuildSegments();
                return;
            }

            // When the head moves forward, the first/tail segment is removed.
            // Remove it from the list and deactivate it immediately so its old
            // collider cannot fire a false trigger during the deferred Destroy.
            while (_spawnedSegments.Count > segmentCount)
            {
                GameObject removed = _spawnedSegments[0];
                _spawnedSegments.RemoveAt(0);
                DeactivateAndDestroy(removed);
            }

            // When a reversed line grows its tail back, one new segment appears
            // at index 0 and all existing segments shift by one. Rebuilding in
            // that case is correct; it is also rare compared to a normal move.
            if (_spawnedSegments.Count < segmentCount)
            {
                if (segmentCount - _spawnedSegments.Count != 1)
                {
                    RebuildSegments();
                    return;
                }

                GameObject added = CreateSegment();
                _spawnedSegments.Insert(0, added);
            }

            for (int i = 0; i < segmentCount && i < _spawnedSegments.Count; i++)
            {
                UpdateSegment(_spawnedSegments[i], i);
            }
        }

        private GameObject CreateSegment()
        {
            GameObject instance = Instantiate(segmentPrefab, transform);
            instance.name = "Segment";
            return instance;
        }

        private void UpdateSegment(GameObject segment, int index)
        {
            if (segment == null) return;

            bool useWorld = lineRenderer.useWorldSpace;
            Vector3 a = lineRenderer.GetPosition(index);
            Vector3 b = lineRenderer.GetPosition(index + 1);

            if (!useWorld)
            {
                a = lineRenderer.transform.TransformPoint(a);
                b = lineRenderer.transform.TransformPoint(b);
            }

            Vector3 dir = b - a;
            float length = dir.magnitude;

            segment.transform.position = (a + b) / 2f;
            segment.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            BoxCollider2D box = segment.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Vector2 size = box.size;
                size.x = length + extraLenght;
                size.y = thickness;
                box.size = size;
                box.offset = Vector2.zero;
            }
        }

        public void ClearSegments()
        {
            foreach (var go in _spawnedSegments)
            {
                DeactivateAndDestroy(go);
            }
            _spawnedSegments.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (child.GetComponent<LineRendererHead>() != null)
                {
                    continue;
                }

                if (child.name.Contains("Clone") || child.name.Contains("Segment") || child.GetComponent<Collider2D>() != null)
                {
                    DeactivateAndDestroy(child.gameObject);
                }
            }
        }

        private static void DeactivateAndDestroy(GameObject go)
        {
            if (go == null) return;

            // Deferred Destroy would otherwise leave the old collider active
            // for the rest of the frame.
            go.SetActive(false);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
                Destroy(go);
#else
            Destroy(go);
#endif
        }
    }
}
