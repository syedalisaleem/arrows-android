using System.Collections.Generic;
using UnityEngine;

namespace _Game.Levels
{
    /// <summary>
    /// Loads the precomputed, solver-verified layouts shipped in
    /// Resources/Levels/procedural_levels.txt.
    ///
    /// One level per line:
    ///     137|0,2:RRUUD;4,-1:LLD
    ///
    /// Each entry is "startX,startY:DIRS", where DIRS is a run of one-unit
    /// orthogonal steps (U/D/L/R). Coordinates are integer grid points.
    /// </summary>
    public static class ProceduralLevelData
    {
        private const string ResourcePath = "Levels/procedural_levels";

        private static Dictionary<int, List<Vector2Int[]>> _levels;

        public static bool IsLoaded => _levels != null && _levels.Count > 0;

        public static int Count
        {
            get
            {
                EnsureLoaded();
                return _levels.Count;
            }
        }

        public static bool TryGet(int levelNumber, out List<Vector2Int[]> lines)
        {
            EnsureLoaded();
            return _levels.TryGetValue(levelNumber, out lines);
        }

        private static void EnsureLoaded()
        {
            if (_levels != null) return;

            _levels = new Dictionary<int, List<Vector2Int[]>>();

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[ProceduralLevelData] Missing Resources/{ResourcePath}.txt");
                return;
            }

            string[] rows = asset.text.Split('\n');
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r].Trim();
                if (row.Length == 0 || row[0] == '#') continue;

                int bar = row.IndexOf('|');
                if (bar <= 0) continue;
                if (!int.TryParse(row.Substring(0, bar), out int number)) continue;

                var lines = new List<Vector2Int[]>();
                string[] entries = row.Substring(bar + 1).Split(';');
                for (int e = 0; e < entries.Length; e++)
                {
                    Vector2Int[] polyline = ParseLine(entries[e]);
                    if (polyline != null && polyline.Length >= 2) lines.Add(polyline);
                }

                if (lines.Count > 0) _levels[number] = lines;
            }

            Debug.Log($"[ProceduralLevelData] Loaded {_levels.Count} procedural levels.");
        }

        private static Vector2Int[] ParseLine(string entry)
        {
            if (string.IsNullOrEmpty(entry)) return null;

            int colon = entry.IndexOf(':');
            if (colon <= 0) return null;

            string[] start = entry.Substring(0, colon).Split(',');
            if (start.Length != 2) return null;
            if (!int.TryParse(start[0], out int x)) return null;
            if (!int.TryParse(start[1], out int y)) return null;

            string steps = entry.Substring(colon + 1);
            var points = new List<Vector2Int>(steps.Length + 1) { new Vector2Int(x, y) };

            for (int i = 0; i < steps.Length; i++)
            {
                switch (steps[i])
                {
                    case 'U': y++; break;
                    case 'D': y--; break;
                    case 'L': x--; break;
                    case 'R': x++; break;
                    default: return null;
                }
                points.Add(new Vector2Int(x, y));
            }

            return points.ToArray();
        }
    }
}
