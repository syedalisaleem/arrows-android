using UnityEngine;

namespace _Game.Levels
{
    /// <summary>
    /// Makes sure the level's backdrop covers the whole camera view.
    ///
    /// Levels are authored with a fixed-size background plane (10x10 units, scaled 2x in
    /// Level_Base), but CameraManager zooms out as the board grows. From roughly level 6
    /// on, the fit needs more than the plane's 20 units of height, so the top and bottom
    /// of the screen fell back to the camera's clear colour - a flat grey strip under the
    /// cream board area. This grows the plane to fit (plus a margin for pinch zoom-out)
    /// and keeps it centred on the camera.
    /// </summary>
    public static class LevelBackgroundFitter
    {
        /// <summary>
        /// Covers the fitted size plus the CameraZoomController's 20% pinch-out, with a
        /// little slack so a one-pixel rounding error can never expose the clear colour.
        /// </summary>
        private const float FitMargin = 1.32f;

        private const string BackgroundName = "Background";

        public static void Fit(Transform levelRoot, Camera camera)
        {
            if (levelRoot == null || camera == null) return;
            if (!camera.orthographic) return;

            Transform background = FindBackground(levelRoot);
            if (background == null) return;

            Renderer backgroundRenderer = background.GetComponent<Renderer>();
            if (backgroundRenderer == null) backgroundRenderer = background.GetComponentInChildren<Renderer>(true);
            if (backgroundRenderer == null) return;

            Bounds bounds = backgroundRenderer.bounds;
            if (bounds.size.x < 0.001f || bounds.size.y < 0.001f) return;

            Vector3 cameraCenter = camera.transform.position;
            float halfHeight = camera.orthographicSize * FitMargin;
            float halfWidth = halfHeight * camera.aspect;

            bool alreadyCovers =
                bounds.min.x <= cameraCenter.x - halfWidth && bounds.max.x >= cameraCenter.x + halfWidth &&
                bounds.min.y <= cameraCenter.y - halfHeight && bounds.max.y >= cameraCenter.y + halfHeight;

            // Hand-authored levels are placed and sized for their own board; leave them be.
            if (alreadyCovers) return;

            // Grow uniformly so the backdrop keeps its proportions, then centre it on the
            // camera so the leftover gap is shared between both edges instead of piling up
            // on one side.
            float scaleX = (halfWidth * 2f) / bounds.size.x;
            float scaleY = (halfHeight * 2f) / bounds.size.y;
            float factor = Mathf.Max(1f, Mathf.Max(scaleX, scaleY));

            background.localScale *= factor;

            bounds = backgroundRenderer.bounds;
            Vector3 shift = cameraCenter - bounds.center;
            background.position += new Vector3(shift.x, shift.y, 0f);
        }

        private static Transform FindBackground(Transform root)
        {
            if (root.name == BackgroundName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindBackground(root.GetChild(i));
                if (found != null) return found;
            }

            return null;
        }
    }
}
