using System.Collections.Generic;
using UnityEngine;
using SerapKeremGameKit._LevelSystem;
using _Game.Line;

namespace _Game.Levels
{
    /// <summary>
    /// Builds a playable level at runtime from a precomputed layout.
    /// Called before Level.Load() so the LineManager and camera see the
    /// finished line set on their normal initialization pass.
    /// </summary>
    public static class ProceduralLevelBuilder
    {
        private const string LinePrefabPath = "Line/Line (1)";
        private const string LinesRootName = "LINES";

        public static bool Build(Level level, int levelNumber)
        {
            if (level == null) return false;

            if (!ProceduralLevelData.TryGet(levelNumber, out List<Vector2Int[]> layout))
            {
                Debug.LogError($"[ProceduralLevelBuilder] No layout found for level {levelNumber}.");
                return false;
            }

            Transform linesParent = level.transform.Find(LinesRootName);
            if (linesParent == null)
            {
                Debug.LogError($"[ProceduralLevelBuilder] '{LinesRootName}' transform not found on level {levelNumber}.");
                return false;
            }

            GameObject linePrefab = Resources.Load<GameObject>(LinePrefabPath);
            if (linePrefab == null)
            {
                Debug.LogError($"[ProceduralLevelBuilder] Missing Resources/{LinePrefabPath}.");
                return false;
            }

            // The base prefab ships with a blank placeholder line. Detach it first so
            // LineManager.InitializeLines() cannot pick it up during Level.Load().
            for (int i = linesParent.childCount - 1; i >= 0; i--)
            {
                Transform child = linesParent.GetChild(i);
                child.SetParent(null, false);
                Object.Destroy(child.gameObject);
            }

            for (int i = 0; i < layout.Count; i++)
            {
                Vector2Int[] points = layout[i];
                GameObject instance = Object.Instantiate(linePrefab, linesParent);
                instance.name = $"Line {i + 1}";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                LineRenderer renderer = instance.GetComponent<LineRenderer>();
                if (renderer == null)
                {
                    Debug.LogError("[ProceduralLevelBuilder] Line prefab has no LineRenderer.");
                    continue;
                }

                // Match the fat, hand-authored stroke scale used by levels 5-10.
                // The camera fit uses roughly two units of padding, so this
                // formula keeps the on-screen stroke close to 40 px across the
                // whole 8x8 .. 24x24 range instead of getting razor-thin late.
                float board = BoardSize(layout);
                float stroke = Mathf.Clamp(0.022f * (board + 4f), 0.28f, 0.55f);
                renderer.widthMultiplier = stroke;

                // Handcrafted levels use a 0.9 triangle on a 0.3 stroke. Scale
                // the procedural head with the stroke, with 0.9 as the floor so
                // early levels never look like needles.
                LineRendererHead head = instance.GetComponentInChildren<LineRendererHead>(true);
                if (head != null)
                {
                    SpriteRenderer headSprite = head.GetComponentInChildren<SpriteRenderer>(true);
                    if (headSprite != null)
                    {
                        float headScale = Mathf.Max(0.9f, stroke * 3f);
                        headSprite.transform.localScale = Vector3.one * headScale;
                        headSprite.sortingOrder = 20;
                    }
                }

                renderer.sortingOrder = 0;
                renderer.useWorldSpace = false;
                renderer.positionCount = points.Length;
                for (int p = 0; p < points.Length; p++)
                {
                    renderer.SetPosition(p, new Vector3(points[p].x, points[p].y, 0f));
                }
            }

            return true;
        }

        private static float BoardSize(List<Vector2Int[]> layout)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            for (int i = 0; i < layout.Count; i++)
            {
                Vector2Int[] points = layout[i];
                for (int p = 0; p < points.Length; p++)
                {
                    minX = Mathf.Min(minX, points[p].x);
                    maxX = Mathf.Max(maxX, points[p].x);
                    minY = Mathf.Min(minY, points[p].y);
                    maxY = Mathf.Max(maxY, points[p].y);
                }
            }

            if (minX == int.MaxValue) return 1f;
            return Mathf.Max(Mathf.Max(maxX - minX, maxY - minY), 1f);
        }
    }
}
