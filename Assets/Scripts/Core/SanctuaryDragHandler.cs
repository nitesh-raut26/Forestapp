using UnityEngine;
using UnityEngine.EventSystems;

namespace ForestFriendsQuest
{
    public class SanctuaryDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public System.Action<Vector2> onDragEnd;

        public void OnDrag(PointerEventData eventData)
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += eventData.delta;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform != null && onDragEnd != null)
            {
                onDragEnd(rectTransform.anchoredPosition);
            }
        }
    }
}
