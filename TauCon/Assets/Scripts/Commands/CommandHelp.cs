using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Taucon
{

    /// <summary>
    /// Adds Help command to the console. Use to get help text of specific commands or a list of available commands.
    /// </summary>
    public class CommandHelp : ScriptableObject
    {

        /// <summary>
        /// Called once in the lifetime of a script, before any Start functions are called.
        /// </summary>
        private void Awake()
        {
            this.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Prints command help texts or calls <see cref="GetHelpList(bool)"/> to get a list of commands and optionally their descriptions.
        /// </summary>
        /// <param name="param">string</param>
        /// <returns>Help text and optionally descriptions.</returns>
        public static string GetHelp(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                return $"Type 'help list [-d]' for a list of commands, or 'help <command>' for a description of a certain command."
                    + $"\n -d | Show list of commands with description.";
            }

            if (param == "me")
            {
                return TauCon.ColorString("No.", TauCon.ExceptionColor);
            }

            if (param == "list -d" || param == "list [-d]")
            {
                return GetHelpList(true);
            }
            else if (param == "list")
            {
                return GetHelpList(false);
            }
            else if (TauCon.Commands.ContainsKey(param))
            {
                Command command = TauCon.Commands[param];
                return $"{(command.helpText == null ? string.Empty : command.helpText)}";
            }
            else if (!TauCon.Commands.ContainsKey(param))
            {
                return $"{param} does not exist.";
            }
            else
            {
                return $"{param} does not have help text.";
            }
        }

        /// <summary>
        /// Returns a list of available commands and optionally their descriptions.
        /// </summary>
        /// <param name="showDescription">bool showDescription</param>
        /// <returns>A string of the requested cmd or cmd + descriptions.</returns>
        private static string GetHelpList(bool showDescription)
        {
            if (!showDescription)
            {
                return TauCon.ColorString(string.Join("\n", TauCon.Commands.Keys.ToArray()), TauCon.HelpColor);
            }
            else
            {
                string result = string.Empty;

                // TODO(Trevor Woodman): Rewrite this foreach loop as a for loop (faster)
                foreach (string command in TauCon.Commands.Keys)
                {
                    result += TauCon.ColorString(command, TauCon.HelpColor) 
                        + " \t" 
                        + TauCon.ColorString(TauCon.Commands[command].description, TauCon.HelpListColor) 
                        + "\n";
                }
                return result;
            }
        }
    }
}
