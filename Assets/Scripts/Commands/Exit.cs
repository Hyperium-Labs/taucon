using System.Collections;
using UnityEngine;

namespace Console.Cmd
{
    /// <summary>
    /// Adds Exit command to console. This will close the console window
    /// </summary>
    public class Exit
    {
        public static string ExitConsole(string param)
        {
            GameObject.Find("TauCon Canvas").SetActive(false);
            return null;
        }
    }
}
