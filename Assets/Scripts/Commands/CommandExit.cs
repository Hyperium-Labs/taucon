using System.Collections;
using UnityEngine;

namespace Taucon
{
    /// <summary>
    /// Adds Exit command to console. This will close the console window.
    /// </summary>
    public class CommandExit : ScriptableObject
    {

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void Awake()
        {
            this.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Exits the console.
        /// </summary>
        /// <param name="param">null</param>
        /// <returns>null</returns>
        public static string ExitConsole(string param)
        {
            GameObject.Find("TauCon Canvas").SetActive(false);
            return null;
        }
    }
}
