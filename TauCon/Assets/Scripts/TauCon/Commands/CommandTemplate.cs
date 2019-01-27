using System.Collections;
using UnityEngine;

namespace TauConsole
{

    /// <summary>
    /// Short description of what the command does.
    /// </summary>
    public class CommandName : ScriptableObject
    {

        private void Awake()
        {
            this.hideFlags = HideFlags.HideAndDontSave;
            // AddCommand() goes in InitDefaultCommands() in TauCon.cs
            // AddCommand("Name", "command", "Very short desc of what it does.", TheMethodItCalls, "Optional help text");
        }

        private string TheMethodItCalls(string param)
        {
            // Method code here
            // if method errors:
            // return [error text]
            // else
            // do whatever the method is made for &&
            // return either no text (null) ||
            // return text that indicates the cmd completed successfully
            return null;
        }
    }
}
