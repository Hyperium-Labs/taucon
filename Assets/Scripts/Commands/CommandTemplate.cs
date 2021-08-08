using System.Collections;
using UnityEngine;

namespace Taucon
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
            

            return null;
        }
    }
}
