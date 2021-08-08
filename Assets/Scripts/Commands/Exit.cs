using System.Collections;
using UnityEngine;

namespace Console.Cmd
{
    /// <summary>
    /// Adds Exit command to console. This will close the console window.
    /// </summary>
    public class Exit
    {
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
