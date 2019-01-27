using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Zuratek.ZGUI
{
    namespace Zuratek.ZGUI
    {
        public class DragPanel : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            private Vector2 _pointerOffset;
            private RectTransform _canvasRectTransform;
            private RectTransform _panelRectTransform;

            private void Awake()
            {
                Canvas canvas = GetComponentInParent<Canvas>();

                if (canvas != null)
                {
                    _canvasRectTransform = (RectTransform)canvas.transform;
                    _panelRectTransform = (RectTransform)transform.parent;
                }
            }

            public void OnPointerDown(PointerEventData data)
            {
                // Set the panel as the top most element (the last UI element in the canvas)
                _panelRectTransform.SetAsLastSibling();
                // Find where pointer came down on panel and out the offset
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRectTransform, data.position, data.pressEventCamera, out _pointerOffset);
            }

            public void OnDrag(PointerEventData data)
            {
                if (_panelRectTransform == null)
                    return;

                // Clamp mouse position to screen
                Vector2 pointerPosition = ClampToWindow(data);

                Vector2 localPointerPosition;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, pointerPosition, data.pressEventCamera, out localPointerPosition))
                {
                    _panelRectTransform.localPosition = localPointerPosition - _pointerOffset;
                }
            }

            private Vector2 ClampToWindow(PointerEventData data)
            {
                Vector2 rawPointerPosition = data.position;

                Vector3[] canvasCorners = new Vector3[4];
                _canvasRectTransform.GetWorldCorners(canvasCorners);

                float clampX = Mathf.Clamp(rawPointerPosition.x, canvasCorners[0].x, canvasCorners[2].x);
                float clampY = Mathf.Clamp(rawPointerPosition.y, canvasCorners[0].y, canvasCorners[2].y);

                Vector2 newPointerPosition = new Vector2(clampX, clampY);

                return newPointerPosition;
            }
        }
    }
}
