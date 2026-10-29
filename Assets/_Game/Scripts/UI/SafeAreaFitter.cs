using UnityEngine;

namespace _Game.UI
{
    /// <summary>
    /// Insets a full-screen UI root by the device's safe area.
    ///
    /// Why this exists: the game is immersive, so Unity draws edge to edge and reports the
    /// display cutout through <see cref="Screen.safeArea"/>. Without this, anything anchored
    /// to the top corners lands under a notch or a punch-hole camera — on a Galaxy S10+ the
    /// "Level N" pill is hidden behind the front camera, for example.
    ///
    /// The component only rewrites its own RectTransform, so child layout is untouched: they
    /// keep anchoring to the corners of the (now inset) rect and simply shift clear of the
    /// cutout. On devices without a cutout the safe area equals the full screen and this is
    /// a no-op.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedSafeArea = new Rect(-1f, -1f, 0f, 0f);
        private int _appliedWidth;
        private int _appliedHeight;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            // Panels are toggled active as the player moves between screens, so re-apply on
            // every show: insets can change while the panel is hidden.
            Apply();
        }

        // Safe area can change while running — rotation, gesture navigation insets, or the
        // app being resized in split screen — so compare instead of applying only once.
        private void Update()
        {
            int w = Screen.width;
            int h = Screen.height;

            if (w != _appliedWidth ||
                h != _appliedHeight ||
                Screen.safeArea != _appliedSafeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rect == null)
            {
                _rect = (RectTransform)transform;
            }

            int width = Screen.width;
            int height = Screen.height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            Rect safe = Screen.safeArea;

            _appliedSafeArea = safe;
            _appliedWidth = width;
            _appliedHeight = height;

            // The rect these anchors describe is the usable area, and the offsets collapse to
            // zero so the panel fills exactly that area rather than adding to it.
            _rect.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
            _rect.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
