using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public class PulseGlow : MonoBehaviour
    {
        public float minAlpha = 0.4f;
        public float maxAlpha = 1f;
        public float speed = 2f;

        private Graphic _graphic;
        private Color _baseColor;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            if (_graphic != null)
            {
                _baseColor = _graphic.color;
            }
        }

        private void Update()
        {
            if (_graphic == null)
            {
                return;
            }

            var t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
            var alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            _graphic.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
        }
    }
}
