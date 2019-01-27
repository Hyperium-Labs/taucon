using System.Collections;
using UnityEngine;

namespace TauConsole
{

    /// <summary>
    /// Adds Quit command to console. This will close the application immediately.
    /// </summary>
    public class CommandQuit : ScriptableObject
    {

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void Awake()
        {
            this.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Closes the application immediately.
        /// </summary>
        /// <param name="param">null</param>
        /// <returns>null</returns>
        public static string QuitApplication(string param)
        {
            if (Application.isEditor)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                return null;
            }
            else
            {
                Application.Quit();
                return null;
            }
        }
    }
}
