using UnityEngine;

namespace ForestFriendsQuest
{
    public class SwayMotion : MonoBehaviour
    {
        public float rotationAmount = 8f;
        public float speed = 1.2f;

        private Quaternion _baseRotation;

        private void Start()
        {
            _baseRotation = transform.localRotation;
        }

        private void Update()
        {
            var angle = Mathf.Sin(Time.unscaledTime * speed) * rotationAmount;
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
        }
    }
}
