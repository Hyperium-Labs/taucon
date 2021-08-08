using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Console.Cmd;

namespace Console
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
        public GameObject MainPanel;
        public ScrollRect OutputLogScrollRect;
        public RectTransform OutputViewport;
        public RectTransform OutputContent;
        public Text OutputLogText;
        public InputField InputField;
        public Text InputText;
        public Scrollbar Scrollbar;
        public RectTransform ScrollbarHandle;
        public Button CloseButton;

        [Header("Console Options")]
        public PrimaryColorTheme ColorTheme;
        public int MaxLines = 5000;
        public int InputCharacterLimit = 60;
        public float CaretBlinkRate = 1f;
        public int CaretWidth = 8;
        public bool ClearInputFieldOnSubmit = true;
        public bool RefocusConsoleOnSubmit = true;
        public bool OutputUnityLog = false;
        public bool OutputStackTrace = false;
        public bool AllowEmptyOutput = true;
        public bool AddNewlineOnOutput = true;
        public bool UseCustomFonts = false;
        public bool UseCustomFontSizes = false;

        [Header("Fonts")]
        public Font OutputTextFont;
        public Font InputTextFont;

        [Header("Font Sizes")]
        public int OutputTextFontSize = 14;
        public int InputTextFontSize = 14;

        #endregion

        public static Color32 LogDefaultColor;
        public static Color32 LogAssertColor;
        public static Color32 LogWarningColor;
        public static Color32 LogErrorColor;
        public static Color32 LogExceptionColor;

        public static string LogDefaultColorHex;
        public static string LogAssertColorHex;
        public static string LogWarningColorHex;
        public static string LogErrorColorHex;
        public static string LogExceptionColorHex;

        public static string LOGERROR;
        public static string LOGWARNING;
        public static string LOGDEFAULT;
        public static string LOGEXCEPTION;
        public static string LOGASSERT;
        public static string LOGCMDINVALID;
        public static string LOGCMDNOTFOUND;
        public static string LOGCMDEXIST;

        private static Color32 _initialInputSelectionColor;
        private static Color32 _initialCaretColor;
        private static int _currentLogHistoryIndex;
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

            _currentLogHistoryIndex = -1;

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
        }

        #endregion

        #region Warn Logging

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

            output += logString + (Instance.OutputStackTrace ? "\n" + trace : String.Empty);
            Print(output);
        }

        #endregion

        #region Adding & Removing ConsoleCommands

        /// <summary>
        /// Removes a command from the Commands Dictionary.
        /// </summary>
        /// <returns>True if command is successfully removed, False if command did not exist.</returns>
        public bool RemoveCommand(string command)
        {
            if (Commands.ContainsKey(command))
            {
                Commands.Remove(command);
                return true;
            }
            Debug.LogError(LOGCMDEXIST + command);
            return false;
        }

        /// <summary>
        /// Add a command from the Commands Dictionary.
        /// </summary>
        /// <param name="name">The capitalized name of the command.</param>
        /// <param name="command">The command string used to invoke the command.</param>
        /// <param name="method">The method to call when the command is invoked.</param>
        /// <param name="helpText">The help text for the command.</param>
        /// <returns>True/False if command is added successfully.</returns>
        public static bool AddCommand(string name, string command, Func<string, string> method, string helpText = "")
        {
            if (Commands.ContainsKey(command))
            {
                Debug.LogError(LOGCMDEXIST + command);
                return false;
            }

            Command consoleCommand = new Command(name, command, method, helpText);
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

            Print(command);

            if (string.IsNullOrEmpty(command))
            {
                if (Instance.OutputUnityLog)
                {
                    Debug.LogError(LOGCMDINVALID + command);
                }
                output = LOGCMDINVALID + command;
                return Print(output);
            }

            command.ToLower();

            string[] parsedCommand = command.Split(' ');
            string rawCommand = parsedCommand[0];
            string trimmedCommand = string.Join(" ", parsedCommand).Trim();

            CommandHistory.Insert(0, trimmedCommand);

            if (!Commands.ContainsKey(rawCommand))
            {
                if (Instance.OutputUnityLog)
                {
                    Debug.LogError(LOGCMDINVALID + rawCommand);
                }
                output = LOGCMDNOTFOUND + rawCommand;
                return Print(output);
            }

            string parameters = ExtractArguments(command, rawCommand);
            output = Commands[rawCommand].method(parameters);

            if (Instance.AddNewlineOnOutput)
            {
                output += "\n";
            }

            _currentLogHistoryIndex = -1;

            return Print(output);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Extract the command and any arguments given.
        /// </summary>
        /// <returns>A list of arguments passed into the command.</returns>
        private static string ExtractArguments(string command, string rawCommand)
        {
            string arguments = (command.Length > rawCommand.Length) ? command.Substring(rawCommand.Length + 1, command.Length - (rawCommand.Length + 1)) : string.Empty;
            return arguments.Trim();
        }

        /// <summary>
        /// Sort all commands alphabetically in the dictionary (for help list).
        /// </summary>
        private static void SortCommands()
        {
            Commands = Commands.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Moves caret to given pos
        /// This sets the colors to transparent for 1 frame to overcome a quirk in Unity's UI
        /// </summary>
        /// <param name="inputField">The input field used</param>
        /// /// <param name="position">The position where to place the caret</param>
        /// <returns>null</returns>
        public static IEnumerator CaretToPosition(InputField inputField, int position)
        {
            inputField.ActivateInputField();
            inputField.selectionColor = new Color32(0, 0, 0, 0);
            inputField.caretColor = new Color32(0, 0, 0, 0);
            yield return null;
            inputField.caretPosition = position;
            inputField.selectionColor = _initialInputSelectionColor;
            inputField.caretColor = _initialCaretColor;
            inputField.Rebuild(CanvasUpdate.PreRender);
        }

        /// <summary>
        /// Rebuilds the output UI to account for log output (resizes the outputContentScrollRect height).
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

        private static string ColorToHex(Color32 color) {
            Color32 color32 = color;
            return String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color32.r, color32.g, color32.b, color32.a);
        }

    #endregion

    #region Printing & Output

    /// <summary>
    /// A method to act on the onEndEdit event for an InputField in Unity, checks for "Submit" event and calls <see cref="TauCon.OnInput()."/>
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
            if (Instance.AddNewlineOnOutput)
            {
                line += "\n";
            }

            LogHistory.Insert(0, line);

            if (LogHistory.Count >= MaxLines)
            {
                LogHistory.RemoveAt(LogHistory.Count - 1);

                OutputLogText.text = null;

                for (int i = LogHistory.Count - 1; i > 0; i--)
                {
                    OutputLogText.text += LogHistory[i];

                }
            }

            // REMOVE
            if (Instance.OutputUnityLog)
            {
                Debug.Log(string.Join(", ", LogHistory.ToArray()));
            }

            OutputLogText.text += line;
            RebuildOutputUI(OutputContent, OutputViewport, Scrollbar, InputField);
        }

        /// <summary>
        /// Called when text has been submitted from the input field.
        /// </summary>
        private void OnInput()
        {
            string command = InputField.text;
            if (string.IsNullOrEmpty(command))
            {
                return;
            }

            Eval(command);

            if (ClearInputFieldOnSubmit)
            {
                InputField.text = string.Empty;
            }

            if (RefocusConsoleOnSubmit)
            {
                InputField.Select();
                InputField.ActivateInputField();
            }

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
        /// /// <param name="color">A colour in hex format.</param>
        /// <returns>Returns either an empty string if text is empty or the text given, optionally coloured.</returns>
        public static string Print(string text, string color = null)
        {
            if (text == null)
            {
                return String.Empty;
            }

            // If allowEmptyOutput is false, do not send to listeners
            if (Instance.AllowEmptyOutput && text == string.Empty)
            {
                return String.Empty;
            }

            SendOutputToListeners(text);

            if (color == null)
            {
                return "<color=#" + LogDefaultColorHex + ">" + text + "</color>";
            }
            else
            {
                return "<color=#" + color + ">" + text + "</color>";
            }
        }

        #endregion

        #region Built-in Console Commands

        /// <summary>
        /// Initialize default commands.
        /// </summary>
        private void InitDefaultCommands()
        {
            AddCommand("Help", "help", Help.GetHelp, "help <command> | Show help text for given command");
            AddCommand("Exit", "exit", Exit.ExitConsole, "Closes the console");
            AddCommand("Clear", "clear", Clear.ClearLog, "Clears the console of all text");
            AddCommand("Commands", "commands", Cmd.Commands.ListCommands, "Lists all available commands");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize custom fonts.
        /// </summary>
        private void InitCustomFonts()
        {
            if (UseCustomFonts)
            {
                OutputLogText.font = OutputTextFont;
                InputText.font = InputTextFont;
            }
        }

        /// <summary>
        /// Initialize font sizes.
        /// </summary>
        private void InitFontSizes()
        {
            if (UseCustomFontSizes)
            {
                OutputLogText.fontSize = OutputTextFontSize;
                InputText.fontSize = InputTextFontSize;
            }
        }

        /// <summary>
        /// Set all default log messages and their colors.
        /// </summary>
        private static void InitDefaultLogMessages()
        {
            LOGCMDINVALID = Print("Command invalid: ", ColorToHex(LogExceptionColor));
            LOGCMDNOTFOUND = Print("Command unrecognized: ", ColorToHex(LogExceptionColor));
            LOGCMDEXIST = Print("Command already exists: ", ColorToHex(LogExceptionColor));
            LOGERROR = Print("Error: ", ColorToHex(LogErrorColor));
            LOGWARNING = Print("Warning: ", ColorToHex(LogWarningColor));
            LOGDEFAULT = Print("Log: ", ColorToHex(LogDefaultColor));
            LOGEXCEPTION = Print("Exception: ", ColorToHex(LogExceptionColor));
            LOGASSERT = Print("Assert: ", ColorToHex(LogAssertColor));
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
        /// Initialize all GUI image color values and settings.
        /// </summary>
        private void InitConsoleGUI()
        {
            InputField.caretBlinkRate = CaretBlinkRate;
            InputField.caretWidth = CaretWidth;
            InputField.customCaretColor = true;

            LogErrorColor = new Color32(233, 133, 128, 255);
            LogExceptionColor = new Color32(233, 133, 128, 255);
            LogWarningColor = new Color32(233, 133, 128, 255);
            LogAssertColor = new Color32(233, 133, 128, 255);

            LogDefaultColorHex = ColorToHex(LogDefaultColor);
            LogErrorColorHex = ColorToHex(LogErrorColor);
            LogExceptionColorHex = ColorToHex(LogExceptionColor);
            LogWarningColorHex = ColorToHex(LogWarningColor);
            LogAssertColorHex = ColorToHex(LogAssertColor);

            switch (ColorTheme)
            {
                case PrimaryColorTheme.Dark:
                    SetConsoleColors(
                        new Color32(43, 43, 43, 255),
                        new Color32(66, 63, 62, 255),
                        new Color32(245, 244, 244, 255),
                        new Color32(233, 133, 128, 255),
                        new Color32(245, 244, 244, 255));
                    LogDefaultColor = new Color32(245, 244, 244, 255);
                    break;
                case PrimaryColorTheme.Light:
                    SetConsoleColors(
                        new Color32(245, 244, 244, 255),
                        new Color32(225, 225, 225, 255),
                        new Color32(43, 43, 43, 255),
                        new Color32(233, 133, 128, 255),
                        new Color32(43, 43, 43, 255));
                    LogDefaultColor = new Color32(43, 43, 43, 255);
                    break;

            }
        }

        /// <summary>
        /// Set console colours based on chosen colour variables.
        /// </summary>
        private void SetConsoleColors(Color32 mainPanelColor, Color32 inputFieldColor, Color32 inputTextColor, Color32 closeButtonColor, Color32 caretColor)
        {
            MainPanel.GetComponent<Image>().color = mainPanelColor;
            InputField.GetComponent<Image>().color = inputFieldColor;
            InputText.color = inputTextColor;
            CloseButton.GetComponent<Image>().color = closeButtonColor;
            InputField.caretColor = caretColor;
        }

        #endregion

        #region Command History

        /// <summary>
        /// Populate InputField with command history.
        /// </summary>
        private void FetchHistory(KeyCode key)
        {
            switch(key)
            {
                case KeyCode.UpArrow:
                    if (_currentLogHistoryIndex < 0)
                    {
                        _currentLogHistoryIndex += 1;
                        InputField.text = CommandHistory[_currentLogHistoryIndex];
                        break;
                    }
                    else if (_currentLogHistoryIndex == CommandHistory.Count - 1)
                    {
                        InputField.text = CommandHistory[CommandHistory.Count - 1];
                        break;
                    }
                    else
                    {
                        _currentLogHistoryIndex += 1;
                        InputField.text = CommandHistory.ElementAt(_currentLogHistoryIndex);
                        break;
                    }
                case KeyCode.DownArrow:
                    if (_currentLogHistoryIndex <= 0)
                    {
                        _currentLogHistoryIndex = -1;
                        InputField.text = "";
                        StartCoroutine(CaretToPosition(InputField, InputField.text.Length));
                        break;
                    }
                    else
                    {
                        _currentLogHistoryIndex -= 1;
                        InputField.text = CommandHistory.ElementAt(_currentLogHistoryIndex);
                        StartCoroutine(CaretToPosition(InputField, 0));
                        break;
                    }
            }
        }

        #endregion
    }
}
