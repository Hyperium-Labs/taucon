using System;

namespace Console
{
    /// <summary>
    /// Constructor class for TauCon commands
    /// </summary>
    public class Command
    {
        public string name;
        public string command;
        public Func<string, string> method;
        public string helpText;

        public Command(string name, string command, Func<string, string> method, string helpText = null)
        {
            this.name = name;
            this.command = command;
            this.method = method;
            this.helpText = helpText;
        }
    }
}

