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
        public char PromptSymbol = '>';
        public PrimaryColorTheme ColorTheme;
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
        public bool CaretCustomColor = false;
        public bool CustomFonts = false;
        public bool CustomFontSizes = false;

        [Header("Fonts")]
        public Font OutputTextFont;
        public Font InputTextFont;

        [Header("Font Sizes")]
        public int OutputTextFontSize = 14;
        public int InputTextFontSize = 14;

        #endregion

        public static string LogCommandColor;
        public static string LogColor;
        public static string LogAssertColor;
        public static string LogWarningColor;
        public static string LogErrorColor;
        public static string LogExceptionColor;

        private static string LOGERROR;
        private static string LOGWARNING;
        private static string LOGDEFAULT;
        private static string LOGEXCEPTION;
        private static string LOGASSERT;
        private static string LOGCMDINVALID;
        private static string LOGCMDNOTFOUND;
        private static string LOGCMDEXIST;

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
        /// <param name="description">A short description of the command.</param>
        /// <param name="method">The method to call when the command is invoked.</param>
        /// <param name="helpText">The help text for the command.</param>
        /// <returns>True/False if command is added successfully.</returns>
        public static bool AddCommand(string name, string command, string description, Func<string, string> method, string helpText = "No help text.")
        {
            if (Commands.ContainsKey(command))
            {
                Debug.LogError(LOGCMDEXIST + command);
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

            Print(Instance.PromptSymbol + " " + ColorString(command, LogCommandColor));

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

            // Check to see if our History array does NOT contain the evaluated cmd
            if (!CommandHistory.Contains(trimmedCommand))
            {
                // If it does not contain it, prepend it
                CommandHistory.Insert(0, trimmedCommand);
            }
            else
            {
                // If it does contain it, remove it from the array and prepend it
                CommandHistory.Remove(trimmedCommand);
                CommandHistory.Insert(0, trimmedCommand);
            }

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

            if (Instance.NewlineOnOutput)
            {
                output += "\n";
            }

            _currentIndex = -1;

            return Print(output);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Used to color text in the logger by wrapping text in color tags.
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
        /// This sets the colors to transparent for 1 frame to overcome a quirk in Unity's UI.
        /// </summary>
        /// <param name="inputField">The input field used.</param>
        /// <returns>null</returns>
        public static IEnumerator CaretToEnd(InputField inputField)
        {
            inputField.ActivateInputField();
            inputField.selectionColor = new Color32(0, 0, 0, 0);
            inputField.caretColor = new Color32(0, 0, 0, 0);
            yield return null;
            inputField.caretPosition = inputField.text.Length;
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
            if (Instance.NewlineOnOutput)
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

            if (ClearOnSubmit)
            {
                InputField.text = string.Empty;
            }

            if (ReselectOnSubmit)
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
        /// Initialize all default commands.
        /// </summary>
        private void InitDefaultCommands()
        {
            AddCommand("Help", "help", "Show help on how to use the console.", CommandHelp.GetHelp, "[arg1] | string (cmd) | Show help text for given command.");
            AddCommand("Exit", "exit", "Closes the console.", CommandExit.ExitConsole);
            AddCommand("Clear", "clear", "Clears the console of all text.", CommandClear.ClearLog);
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
                OutputLogText.font = OutputTextFont;
                InputText.font = InputTextFont;
            }
        }

        /// <summary>
        /// Set all font sizes.
        /// </summary>
        private void InitFontSizes()
        {
            if (CustomFontSizes)
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
            LOGCMDINVALID = ColorString("Command invalid: ", LogExceptionColor);
            LOGCMDNOTFOUND = ColorString("Command unrecognized: ", LogExceptionColor);
            LOGCMDEXIST = ColorString("Command already exists: ", LogExceptionColor);
            LOGERROR = ColorString("Error: ", LogErrorColor);
            LOGWARNING = ColorString("Warning: ", LogWarningColor);
            LOGDEFAULT = ColorString("Log: ", LogColor);
            LOGEXCEPTION = ColorString("Exception: ", LogExceptionColor);
            LOGASSERT = ColorString("Assert: ", LogAssertColor);
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

            InputField.caretBlinkRate = CaretBlinkRate;
            InputField.caretWidth = CaretWidth;
            InputField.customCaretColor = CaretCustomColor;
        }

        /// <summary>
        /// Set console colours based on chosen colour variables.
        /// </summary>
        /// <param name="mainPanelColor"></param>
        /// <param name="inputFieldColor"></param>
        /// <param name="inputTextColor"></param>
        /// <param name="closeButtonColor"></param>
        /// <param name="caretColor"></param>
        private void SetConsoleColors(Color32 mainPanelColor, Color32 inputFieldColor, Color32 inputTextColor, Color32 closeButtonColor, Color32 caretColor)
        {
            MainPanel.GetComponent<Image>().color = new Color32(mainPanelColor.r, mainPanelColor.g, mainPanelColor.b, 255);
            InputField.GetComponent<Image>().color = new Color32(inputFieldColor.r, inputFieldColor.g, inputFieldColor.b, 255);
            InputText.color = new Color32(inputTextColor.r, inputTextColor.g, inputTextColor.b, 255);
            CloseButton.GetComponent<Image>().color = new Color32(closeButtonColor.r, closeButtonColor.g, closeButtonColor.b, 200);
            InputField.caretColor = new Color32(caretColor.r, caretColor.g, caretColor.b, 255);
        }

        #endregion

        #region Command History

        /// <summary>
        /// Populate InputField with command history.
        /// </summary>
        /// <param name="key">KeyCode</param>
        private void FetchHistory(KeyCode key)
        {
            switch(key)
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
