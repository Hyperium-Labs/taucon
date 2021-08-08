using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Taucon
{

    [AddComponentMenu("Scripts/Taucon/Console")]
    /// <summary>
    /// This script must be attached to the main TauCon Canvas
    /// Default UI Element Name: Console
    /// </summary>
    public class TauCon : MonoBehaviour
    {

        #region Theme Enums

        public enum PrimaryColorTheme
        {
            Dark,
            Light
        }

        #endregion

        #region User Variables

        [Header("UI Components")]
        public Canvas Canvas;
        public GameObject VersionPanel;
        public GameObject OutputPanel;
        public ScrollRect OutputLogScrollRect;
        public RectTransform OutputViewport;
        public RectTransform OutputContent;
        public Text OutputLogText;
        public InputField InputField;
        public Text InputText;
        public Text InputPlaceholderText;
        public Scrollbar Scrollbar;
        public RectTransform ScrollbarHandle;
        public Button ForceExitButton;

        [Header("Console Options")]
        public char PromptSymbol = '>';
        public PrimaryColorTheme ColorTheme;
        [Tooltip("This means you are going to set your own colours.")]
        public bool UseCustomTheme = false;
        public int MaxLines = 5000;
        public int InputCharacterLimit = 60;
        public float CaretBlinkRate = 1.5f;
        public int CaretWidth = 10;
        public bool ClearOnSubmit = true;
        public bool ReselectOnSubmit = false;
        public bool TabFocus = true;
        public bool OutputUnityLog = false;
        public bool OutputStackTrace = false;
        public bool AllowEmptyOutput = true;
        public bool NewlineOnOutput = true;
        public bool CaretCustomColor = true;
        public bool CustomFonts = false;
        public bool CustomFontSizes = false;

        [Header("Fonts")]
        public Font VersionTextFont;
        public Font OutputLogTextFont;
        public Font InputTextFont;
        public Font InputPlaceholderTextFont;

        [Header("Font Sizes")]
        public int VersionTextFontSize = 14;
        public int OutputLogTextFontSize = 14;
        public int InputTextFontSize = 14;
        public int InputPlaceholderTextFontSize = 14;

        #endregion

        public static string CommandColor;
        public static string LogColor;
        public static string AssertColor;
        public static string WarningColor;
        public static string ErrorColor;
        public static string ExceptionColor;

        private static Color32 _initialInputSelectionColor;
        private static Color32 _initialCaretColor;
        private static int _currentIndex;
#pragma warning disable
        private float _outputContentHeight;
#pragma warning enable

        private Vector2 _outputContentReset = new Vector2(0f, 0f);

        public static TauCon Instance;
        public static Dictionary<string, Command> Commands = new Dictionary<string, Command>();
        public static List<string> CommandHistory = new List<string>();
        public static List<string> LogHistory = new List<string>();
        public delegate void ConsoleListener(string line);
        public static event ConsoleListener OnOutputEvent;

        static TauCon() { }

        #region Unity Callbacks

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            Commands = new Dictionary<string, Command>();
        }

        private void Start()
        {
            OnOutputEvent += OnOutput;
            if (OutputUnityLog)
            {
                Application.logMessageReceived += new Application.LogCallback(this.HandleUnityLog);
            }

            // Init current index for History
            _currentIndex = -1;

            InitCustomFonts();
            InitFontSizes();
            InitConsoleGUI();
            InitConsoleOptions();
            InitDefaultLogMessages();
            InitDefaultCommands();

            InputField.onEndEdit.AddListener(OnEndEdit);
            _outputContentHeight = OutputContent.rect.height;

            OutputLogText.text = string.Empty;
        }

        /// <summary>
        /// Called every frame, but update interval times will vary depending on FPS.
        /// </summary>
        private void Update()
        {
            // Check for active console and 'return' event for command input
            if (Canvas.gameObject.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    // Only parse text if there is text
                    if (InputText.text != "")
                    {
                        // Clear the console input field
                        InputText.text = null;
                    }
                }
            }

            // Check for up/down arrow for History
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                FetchHistory(KeyCode.UpArrow);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                FetchHistory(KeyCode.DownArrow);
            }

            // Check for tab hit
            if (Input.GetKeyDown(KeyCode.Tab) && TabFocus && !InputField.isFocused)
            {
                TauCon.Instance.InputField.ActivateInputField();
            }
        }

        #endregion

        #region Warn Logging

        private static string LOGERROR;
        private static string LOGWARNING;
        private static string LOGDEFAULT;
        private static string LOGEXCEPTION;
        private static string LOGASSERT;
        private static string LOGINVALIDCMD;
        private static string LOGCMDNOTFOUND;

        private void HandleUnityLog(string logString, string trace, LogType logType)
        {
            string output = String.Empty;

            switch (logType)
            {
                case LogType.Error:
                    output += LOGERROR;
                    break;
                case LogType.Assert:
                    output += LOGASSERT;
                    break;
                case LogType.Warning:
                    output += LOGWARNING;
                    break;
                case LogType.Log:
                    output += LOGDEFAULT;
                    break;
                case LogType.Exception:
                    output += LOGEXCEPTION;
                    break;
                default:
                    return;
            }

            output += logString + (Instance.OutputStackTrace ? "\n" + ColorString(trace, LogColor) : String.Empty);
            Print(output);
        }

        #endregion

        #region Adding & Removing ConsoleCommands

        /// <summary>
        /// Removes a command from the Commands Dictionary.
        /// </summary>
        /// <param name="command">The command to remove from the Commands Dictionary.</param>
        /// <returns>True/False if Commands contains given command.</returns>
        public bool RemoveCommand(string command)
        {
            if (Commands.ContainsKey(command))
            {
                Commands.Remove(command);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a command from the Commands Dictionary.
        /// </summary>
        /// <param name="name">The capitalized name of the command.</param>
        /// <param name="command">The command string used to invoke the command.</param>
        /// <param name="description">A short description of the command.</param>
        /// <param name="method">The method to call when the command is invoked.</param>
        /// <param name="helpText">The help text for the command.</param>
        /// <returns>True/False if command is added successfully.</returns>
        public static bool AddCommand(string name, string command, string description, Func<string, string> method, string helpText = "No help text.")
        {
            if (string.IsNullOrEmpty(command))
            {
                Debug.LogError("Could not add command to console { " + command + " }, command is empty");
                return false;
            }

            if (Commands.ContainsKey(command))
            {
                Debug.LogError("Could not add command to console { " + command + " }, command already exists");
                return false;
            }

            Command consoleCommand = new Command(name, command, description, method, helpText);
            Commands.Add(command, consoleCommand);

            SortCommands();

            return true;
        }

        #endregion

        #region Command Eval

        /// <summary>
        /// Evaluate given string (execute command)
        /// <returns> Direct output of the method that is called</returns>
        public static string Eval(string command)
        {

            string output = string.Empty;

            Print(Instance.PromptSymbol + " " + ColorString(command, CommandColor));

            if (string.IsNullOrEmpty(command))
            {
                Debug.Log("Invalid Cmd");
                output = LOGINVALIDCMD;
                return Print(output);
            }

            command.ToLower();

            string[] parsedCmd = command.Split(' ');

            string rawCmd = parsedCmd[0];
            string trimmedCmd = string.Join(" ", parsedCmd).Trim();

            // Check to see if our History array does NOT contain the evaluated cmd
            if (!CommandHistory.Contains(trimmedCmd))
            {
                // If it does not contain it, prepend it
                CommandHistory.Insert(0, trimmedCmd);
            }
            else
            {
                // If it does contain it, remove it from the array and prepend it
                CommandHistory.Remove(trimmedCmd);
                CommandHistory.Insert(0, trimmedCmd);
            }

            if (!Commands.ContainsKey(rawCmd))
            {
                Debug.Log("Command not found");
                output = LOGCMDNOTFOUND + rawCmd;
                return Print(output);
            }

            string parameters = ExtractParameters(command, rawCmd);
            output = Commands[rawCmd].method(parameters);

            if (Instance.NewlineOnOutput)
            {
                output += "\n";
            }

            _currentIndex = -1;

            return Print(output);
        }

        #endregion

        #region Utility Methods

        /// <summary>Used to color text in the logger by wrapping text in color tags
        /// <para>string text</para>
        /// <para>[string color(hex) = null]</para>
        /// </summary>
        public static string ColorString(string text, string color = null)
        {
            if (color == null)
            {
                return "<color=#" + LogColor + ">" + text + "</color>";
            }
            else
            {
                return "<color=#" + color + ">" + text + "</color>";
            }
        }

        /// <summary>
        /// Extract the command and any parameters given.
        /// </summary>
        /// <param name="command">The command used.</param>
        /// <param name="rawCmd">The raw command without parameters.</param>
        /// <returns></returns>
        private static string ExtractParameters(string command, string rawCmd)
        {
            string parameters = (command.Length > rawCmd.Length) ? command.Substring(rawCmd.Length + 1, command.Length - (rawCmd.Length + 1)) : string.Empty;
            return parameters.Trim();
        }

        /// <summary>
        /// Sort the commands alphabetically in the dictionary (for help list)
        /// </summary>
        private static void SortCommands()
        {
            Commands = Commands.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Moves caret to given pos
        /// This sets the colors to transparent for 1 frame to overcome a quirk in Unity's UI.
        /// </summary>
        /// <param name="inputField">The input field used.</param>
        /// <returns>null</returns>
        public static IEnumerator CaretToEnd(InputField inputField)
        {
            // Focus the given input field
            inputField.ActivateInputField();
            // Hide the selection color
            inputField.selectionColor = new Color32(0, 0, 0, 0);
            inputField.caretColor = new Color32(0, 0, 0, 0);
            // Wait for 1 frame
            yield return null;
            // Move the input field caret to the end of the text
            inputField.caretPosition = inputField.text.Length;
            // Reset selection color to initial color
            inputField.selectionColor = _initialInputSelectionColor;
            inputField.caretColor = _initialCaretColor;
            inputField.Rebuild(CanvasUpdate.PreRender);
        }

        /// <summary>
        /// Rebuilds the output UI to account for log output (resizes the outputContentScrollRect height)
        /// <para>RectTransform content</para>
        /// <para>RectTransform parent</para>
        /// <para>Scrollbar scrollbar</para>
        /// </summary>
        public void RebuildOutputUI(RectTransform content, RectTransform parent, Scrollbar scrollbar, InputField inputField)
        {
            // Rebuild content RT
            content.GetComponent<RectTransform>().anchoredPosition = parent.position;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            content.offsetMin = _outputContentReset;
            content.offsetMax = _outputContentReset;
            content.transform.SetParent(parent);

            // Rebuild scrollbar
            scrollbar.Rebuild(CanvasUpdate.Prelayout);

            // Rebuild InputField
            inputField.Rebuild(CanvasUpdate.PreRender);
        }

        #endregion

        #region Printing & Output

        /// <summary>
        /// A method to act on the onEndEdit event for an InputField in Unity, checks for "Submit" event and calls tauConGUI.OnInput()
        /// </summary>
        /// <param name="line"></param>
        private void OnEndEdit(string line)
        {
            if (Input.GetButtonDown("Submit"))
            {
                OnInput();
            }
        }

        /// <summary>
        /// Called when text is to be appended to the output log.
        /// </summary>
        /// <param name="line">The line to append to the output log.</param>
        private void OnOutput(string line)
        {
            if (Instance.NewlineOnOutput)
            {
                line += "\n";
            }

            // Push log to LogHistory
            LogHistory.Insert(0, line);

            if (LogHistory.Count >= MaxLines)
            {
                // Remove the last logged item in the list
                LogHistory.RemoveAt(LogHistory.Count - 1);

                // clear output log
                OutputLogText.text = null;

                for (int i = LogHistory.Count - 1; i > 0; i--)
                {
                    OutputLogText.text += LogHistory[i];

                }
            }

            Debug.Log(string.Join(", ", LogHistory.ToArray()));

            OutputLogText.text += line;
            RebuildOutputUI(OutputContent, OutputViewport, Scrollbar, InputField);
        }

        /// <summary>
        /// Called when text has been submitted from the input field.
        /// </summary>
        private void OnInput()
        {
            // Get the value of the input field
            string command = InputField.text;
            // If there's no command, return
            if (string.IsNullOrEmpty(command))
            {
                return;
            }

            // Otherwise continue...
            // Send command to console & eval
            Eval(command);

            // If clearOnSubmit is enabled
            if (ClearOnSubmit)
            {
                // Clear the input field
                InputField.text = string.Empty;
            }
            // If reselectOnSubmit is enabled
            if (ReselectOnSubmit)
            {
                InputField.Select();
                InputField.ActivateInputField();
            }
            // And then rebuild the UI elements that need to be rebuilt to show changes
            RebuildOutputUI(OutputContent, OutputViewport, Scrollbar, InputField);
        }

        private static string SendOutputToListeners(string output)
        {
            if (OnOutputEvent != null)
            {
                OnOutputEvent(output);
            }
            return output;
        }

        /// <summary>
        /// Send text to listeners and return text.
        /// </summary>
        /// <remarks>Overrides MonoBehaviour's Print method.</remarks>
        /// <param name="text">The string of text to send.</param>
        /// <returns>Returns either an empty string if text is empty or the text given.</returns>
        public static string Print(string text)
        {
            if (text == null)
            {
                return String.Empty;
            }

            // If allowEmptyOutput is false, do not send to listeners
            if (TauCon.Instance.AllowEmptyOutput && text == string.Empty)
            {
                return String.Empty;
            }

            SendOutputToListeners(text);
            return text;
        }

        #endregion

        #region Built-in Console Commands

        /// <summary>
        /// Initialize all default commands 
        /// </summary>
        private void InitDefaultCommands()
        {
            AddCommand("Help", "help", "Show help on how to use the console.", CommandHelp.GetHelp, "[arg1] | string (cmd) | Show help text for given command.");

            AddCommand("Exit", "exit", "Exits the application.", CommandQuit.QuitApplication, "noargs");

            AddCommand("Clear", "clear", "Clears the output log of all text.", CommandClear.ClearLog, "noargs");

            AddCommand("Volume", "volume", "Set volume value to a float ranging from 0.0 to 1.0.",
                CommandVolume.ChangeVolume, "[arg1] | float (0.0 to 1.0) | Set volume value.");

            AddCommand("Exit", "exit", "Close the console.", CommandExit.ExitConsole, "noargs");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Set all custom fonts.
        /// </summary>
        private void InitCustomFonts()
        {
            if (CustomFonts)
            {
                OutputLogText.font = OutputLogTextFont;
                InputText.font = InputTextFont;
                InputPlaceholderText.font = InputPlaceholderTextFont;
            }
        }

        /// <summary>
        /// Set all font sizes.
        /// </summary>
        private void InitFontSizes()
        {
            if (CustomFontSizes)
            {
                OutputLogText.fontSize = OutputLogTextFontSize;
                InputText.fontSize = InputTextFontSize;
                InputPlaceholderText.fontSize = InputPlaceholderTextFontSize;
            }
        }

        /// <summary>
        /// Set all default log messages and their colors.
        /// </summary>
        private static void InitDefaultLogMessages()
        {
            LOGINVALIDCMD = ColorString("Command invalid: ", ExceptionColor);
            LOGCMDNOTFOUND = ColorString("Command unrecognized: ", ExceptionColor);
            LOGERROR = ColorString("Error: ", ErrorColor);
            LOGWARNING = ColorString("Warning: ", WarningColor);
            LOGDEFAULT = ColorString("Log: ", LogColor);
            LOGEXCEPTION = ColorString("Exception: ", ExceptionColor);
            LOGASSERT = ColorString("Assert: ", AssertColor);
        }

        /// <summary>
        /// Initialize all console options.
        /// </summary>
        private void InitConsoleOptions()
        {
            InputField.characterLimit = InputCharacterLimit;
            _initialInputSelectionColor = InputField.selectionColor;
            _initialCaretColor = InputField.caretColor;
        }

        /// <summary>
        /// Sets all GUI image color values and settings.
        /// </summary>
        private void InitConsoleGUI()
        {
            if (!UseCustomTheme)
            {
                switch (ColorTheme)
                {
                    case PrimaryColorTheme.Dark:
                        SetConsoleColors(
                            new Color32(46, 46, 46, 255),
                            new Color32(58, 58, 58, 255),
                            new Color32(73, 73, 73, 255),
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255));
                        break;
                    case PrimaryColorTheme.Light:
                        SetConsoleColors(
                            new Color32(158, 158, 158, 255),
                            new Color32(224, 224, 224, 255),
                            new Color32(238, 238, 238, 255),
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255));
                        break;
                }
            }

            InputField.caretBlinkRate = CaretBlinkRate;
            InputField.caretWidth = CaretWidth;
            InputField.customCaretColor = CaretCustomColor;
        }

        private void SetConsoleColors(Color32 outputPanelColor, Color32 inputFieldColor, Color32 inputTextColor, Color32 forceExitButton, Color32 caretColor)
        {
            OutputPanel.GetComponent<Image>().color = new Color32(outputPanelColor.r, outputPanelColor.g, outputPanelColor.b, 255);
            InputField.GetComponent<Image>().color = new Color32(inputFieldColor.r, inputFieldColor.g, inputFieldColor.b, 255);
            InputText.color = new Color32(inputTextColor.r, inputTextColor.g, inputTextColor.b, 255);
            ForceExitButton.GetComponent<Image>().color = new Color32(forceExitButton.r, forceExitButton.g, forceExitButton.b, 200);
            InputField.caretColor = new Color32(caretColor.r, caretColor.g, caretColor.b, 255);
        }

        #endregion

        #region Command History

        /// <summary>
        /// Populate InputField based on command history
        /// </summary>
        /// <param name="dir">KeyCode</param>
        private void FetchHistory(KeyCode dir)
        {
            switch(dir)
            {
                case KeyCode.UpArrow:
                    if (_currentIndex < 0)
                    {
                        _currentIndex += 1;
                        InputField.text = CommandHistory[_currentIndex];
                        break;
                    }
                    else if (_currentIndex == CommandHistory.Count - 1)
                    {
                        InputField.text = CommandHistory[CommandHistory.Count - 1];
                        break;
                    }
                    else
                    {
                        _currentIndex += 1;
                        InputField.text = CommandHistory.ElementAt(_currentIndex);
                        break;
                    }
                case KeyCode.DownArrow:
                    if (_currentIndex <= 0)
                    {
                        _currentIndex = -1;
                        InputField.text = "";
                        StartCoroutine(CaretToEnd(InputField));
                        break;
                    }
                    else
                    {
                        _currentIndex -= 1;
                        InputField.text = CommandHistory.ElementAt(_currentIndex);
                        StartCoroutine(CaretToEnd(InputField));
                        break;
                    }
            }
        }

        #endregion
    }
}
