using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public class ButtonBehaviour : MonoBehaviour
    {
        public GameObject Canvas;
        public RectTransform Console;

        public void CloseConsole()
        {
            Canvas.SetActive(!Canvas.activeSelf);
        }

        public void ResetConsolePosition()
        {
            Console.anchoredPosition = Vector3.zero;
        }
    }
}