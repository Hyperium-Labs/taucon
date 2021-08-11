using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Console.Cmd
{

    /// <summary>
    /// Clears the output log of all text
    /// </summary>
    public class Clear
    {
        public static string ClearLog(string param)
        {
            Taucon.Instance.OutputLogText.text = "";
            return null;
        }
    }
}
