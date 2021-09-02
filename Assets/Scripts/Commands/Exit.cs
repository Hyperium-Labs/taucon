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
            try
            {
                GameObject.FindWithTag("Taucon").SetActive(false);
                Clear.ClearLog(null);
                return null;
            } catch
            {
                return "Couldn't find the console, does the Taucon canvas object have the 'Taucon' tag?";
            }
            
        }
    }
}
