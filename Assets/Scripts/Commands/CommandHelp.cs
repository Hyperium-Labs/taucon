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
            hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Prints command help texts or calls <see cref="ListCommands()"/> to get a list of commands.
        /// </summary>
        /// <param name="param">string</param>
        /// <returns>A string of the commands help text, if it exists.</returns>
        public static string GetHelp(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                return "Type 'help <command>' for help with a specific command.";
            }

            if (!TauCon.Commands.ContainsKey(param))
            {
                return $"{param} does not exist.";
            }
            else if (TauCon.Commands.ContainsKey(param))
            {
                Command command = TauCon.Commands[param];
                return $"{command.helpText}";
            }
            else
            {
                return $"{param} does not have help text.";
            }
        }

        /// <summary>
        /// Returns a list of available commands.
        /// </summary>
        /// <returns>A list of all available commands.</returns>
        private static string ListCommands()
        {
            string result = string.Empty;

            foreach (string command in TauCon.Commands.Keys)
            {
                result += TauCon.ColorString(command, TauCon.LogColor)
                    + " \t";
            }
            return result;
        }
    }
}
