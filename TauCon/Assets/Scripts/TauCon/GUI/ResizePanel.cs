using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Common.TGUI
{
    public class ResizePanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Vector2 MinSize;
        public Vector2 MaxSize;

        private RectTransform _rectTransform;
        private Vector2 _currentPointerPosition;
        private Vector2 _previousPointerPosition;

        private void Awake()
        {
            _rectTransform = transform.parent.GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData data)
        {
            // Set the console as last sibling in canvas so it appears overtop of other UI elements
            _rectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, data.position, data.pressEventCamera, out _previousPointerPosition);
        }

        public void OnDrag(PointerEventData data)
        {
            if (_rectTransform == null)
                return;

            if (_rectTransform.sizeDelta.x == MinSize.x || _rectTransform.sizeDelta.x == MaxSize.x)
            {

            }

            Vector2 sizeDelta = _rectTransform.sizeDelta;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, data.position, data.pressEventCamera, out _currentPointerPosition);
            Vector2 resizeValue = _currentPointerPosition - _previousPointerPosition;

            sizeDelta += new Vector2(resizeValue.x, -resizeValue.y);
            sizeDelta = new Vector2(
                Mathf.Clamp(sizeDelta.x, MinSize.x, MaxSize.x),
                Mathf.Clamp(sizeDelta.y, MinSize.y, MaxSize.y));
            _rectTransform.sizeDelta = sizeDelta;
            _previousPointerPosition = _currentPointerPosition;
        }
    }
}
