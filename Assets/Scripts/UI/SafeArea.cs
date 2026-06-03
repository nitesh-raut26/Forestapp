using UnityEngine;

namespace ForestFriendsQuest
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
        private Vector2Int _lastScreenSize = new Vector2Int(0, 0);
        private ScreenOrientation _lastOrientation = ScreenOrientation.Unknown;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (safeArea != _lastSafeArea || screenSize != _lastScreenSize || Screen.orientation != _lastOrientation)
            {
                _lastSafeArea = safeArea;
                _lastScreenSize = screenSize;
                _lastOrientation = Screen.orientation;
                ApplySafeArea(safeArea);
            }
        }

        private void ApplySafeArea(Rect r)
        {
            if (_rectTransform == null) return;

            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
        }
    }
}
