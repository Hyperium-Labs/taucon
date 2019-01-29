using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TauConsole
{

    /// <summary>
    /// Short description of what the command does.
    /// </summary>
    public class CommandClear : ScriptableObject
    {

        private void Awake()
        {
            this.hideFlags = HideFlags.HideAndDontSave;
        }

        public static string ClearLog(string param)
        {
            TauCon.Instance.OutputLogText.text = "";
            return null;
        }
    }
}
