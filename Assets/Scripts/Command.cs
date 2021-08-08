using System;

namespace Taucon
{
    /// <summary>
    /// Constructor class for TauCon commands.
    /// </summary>
    public class Command
    {
        public string name;
        public string command;
        public string description;
        public Func<string, string> method;
        public string helpText;

        public Command(string name, string command, string description, Func<string, string> method, string helpText = null)
        {
            this.name = name;
            this.command = command;
            this.description = description;
            this.method = method;
            this.helpText = helpText;
        }
    }
}

