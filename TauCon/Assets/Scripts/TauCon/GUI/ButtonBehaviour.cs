using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        StartCoroutine(ResetPos());
        Console.transform.position = Vector3.zero;
    }

    IEnumerator ResetPos()
    {
        yield return new WaitForEndOfFrame();
        Console.transform.position = Vector3.zero;
    }
}
