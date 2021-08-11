using System.Collections;
using UnityEngine;

namespace Console.Cmd
{

    /// <summary>
    /// Short description of what the command does
    /// </summary>
    public class Template
    {
        // AddCommand() goes in InitDefaultCommands() in TauCon.cs
        // AddCommand("Name", "command", TheMethodItCalls(), "Optional help text");
        private string TheMethodItCalls(string param)
        {
            return null;
        }
    }
}
