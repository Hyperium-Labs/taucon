using System.Collections;
using UnityEngine;

namespace Console.Cmd
{

    /// <summary>
    /// Lists all available commands
    /// </summary>
    public class Commands : ScriptableObject
    {

        private void Awake()
        {
            this.hideFlags = HideFlags.HideAndDontSave;
            
        }

        public static string ListCommands(string param)
        {
            string result = string.Empty;

            foreach (string command in Taucon.Commands.Keys)
            {
                result += command + "\t";
            }
            return result;
        }
    }
}
