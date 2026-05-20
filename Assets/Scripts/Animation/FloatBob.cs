using UnityEngine;

namespace ForestFriendsQuest
{
    public class FloatBob : MonoBehaviour
    {
        public float amplitude = 8f;
        public float speed = 1.6f;

        private RectTransform _rectTransform;
        private Vector2 _startPosition;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
        }

        private void Start()
        {
            if (_rectTransform != null)
            {
                _startPosition = _rectTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (_rectTransform == null)
            {
                return;
            }

            var offset = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
            _rectTransform.anchoredPosition = _startPosition + new Vector2(0f, offset);
        }
    }
}
