using UnityEngine;

namespace Console.Cmd
{

    /// <summary>
    /// Adds Help command to the console. Use to get help text of specific commands or a list of available commands
    /// </summary>
    public class Help
    {
        public static string GetHelp(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                return "Type 'help <command>' for help with a specific command.";
            }

            if (!TauCon.Commands.ContainsKey(param))
            {
                return $"{TauCon.LOGCMDINVALID + param}";
            }
            else if (TauCon.Commands.ContainsKey(param))
            {
                Command command = TauCon.Commands[param];
                if (command.helpText == string.Empty)
                {
                    return $"{param} does not have help text.";
                }
                return $"{command.helpText}";
            }
            else
            {
                return $"{TauCon.LOGERROR} + Unspecified";
            }
        }
    }
}
