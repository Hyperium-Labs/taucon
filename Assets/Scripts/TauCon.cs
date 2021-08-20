using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Console.Cmd;

namespace Console
{

    [AddComponentMenu("Baphomet Labs/Taucon/Console")]
    /// <summary>
    /// This script must be attached to the main TauCon Canvas
    /// Default UI Element Name: Console
    /// </summary>
    public class Taucon : MonoBehaviour
    {

        public enum PrimaryColorTheme
        {
            Dark,
            Light
        }

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
        public string CommandOutputMarker = "";
        public string LogOutputMarker = "";
        public bool ClearInputFieldOnSubmit = true;
        public bool RefocusConsoleOnSubmit = true;
        public bool OutputUnityLog = false;
        public bool OutputStackTrace = false;
        public bool AddNewlineOnOutput = true;
        public bool UseCustomFonts = false;
        public bool UseCustomFontSizes = false;

        [Header("Custom Fonts")]
        [Tooltip("Using this will override the default font.")]
        public Font OutputTextFont;
        [Tooltip("Using this will override the default font.")]
        public Font InputTextFont;

        [Header("Custom Font Sizes")]
        public int OutputTextFontSize = 14;
        public int InputTextFontSize = 14;

        #endregion

        private static Color32 LogDefaultColor;
        private static Color32 LogErrorColor;

        public static string LogDefaultColorHex;
        public static string LogErrorColorHex;

        public static string LOGERROR;
        public static string LOGDEFAULT;
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

        public static Taucon Instance;
        public static Dictionary<string, Command> Commands = new Dictionary<string, Command>();
        public static List<string> CommandHistory = new List<string>();
        public static List<string> LogHistory = new List<string>();
        public delegate void ConsoleListener(string line);
        public static event ConsoleListener OnOutputEvent;

        static Taucon() { }

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
            if (OutputUnityLog)
            {
                Application.logMessageReceived += new Application.LogCallback(HandleUnityLog);
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
        /// Called every frame, but update interval times will vary depending on FPS
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

        private static void HandleUnityLog(string logString, string trace, LogType logType)
        {
            string output = String.Empty;
            Color32 color = default(Color32);

            switch (logType)
            {
                case LogType.Error:
                    output += LOGERROR;
                    color = LogErrorColor;
                    break;
                case LogType.Assert:
                    color = LogErrorColor;
                    break;
                case LogType.Warning:
                    color = LogErrorColor;
                    break;
                case LogType.Log:
                    color = LogDefaultColor;
                    break;
                case LogType.Exception:
                    color = LogErrorColor;
                    break;
                default:
                    return;
            }

            output += logString + (Instance.OutputStackTrace ? "\n" + trace : String.Empty);
            Print(output, color);
        }

        #endregion

        #region Adding & Removing ConsoleCommands

        /// <summary>
        /// Removes a command from the Commands Dictionary
        /// </summary>
        /// <returns>True if command is successfully removed, False if command did not exist</returns>
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
        /// Add a command from the Commands Dictionary
        /// </summary>
        /// <param name="name">The capitalized name of the command</param>
        /// <param name="command">The command string used to invoke the command</param>
        /// <param name="method">The method to call when the command is invoked</param>
        /// <param name="helpText">The help text for the command</param>
        /// <returns>True/False if command is added successfully</returns>
        public static bool AddCommand(string name, string command, Func<string, string> method, string helpText = "")
        {
            if (Commands.ContainsKey(command))
            {
                Debug.LogError(LOGCMDEXIST + command);
                return false;
            }

            Command consoleCommand = new Command(name, command, method, helpText);
            Commands.Add(command, consoleCommand);

            return true;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Extract the command and any arguments given
        /// </summary>
        /// <returns>A list of arguments passed into the command</returns>
        private static string ExtractArguments(string command, string rawCommand)
        {
            string arguments = (command.Length > rawCommand.Length) ? command.Substring(rawCommand.Length + 1, command.Length - (rawCommand.Length + 1)) : string.Empty;
            return arguments.Trim();
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
        }

        /// <summary>
        /// Rebuilds the output UI to account for log output (resizes the outputContentScrollRect height)
        /// </summary>
        public void RebuildOutputUI(RectTransform content, RectTransform parent, Scrollbar scrollbar, InputField inputField)
        {
            content.GetComponent<RectTransform>().anchoredPosition = parent.position;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            content.offsetMin = _outputContentReset;
            content.offsetMax = _outputContentReset;
            content.transform.SetParent(parent);
            scrollbar.Rebuild(CanvasUpdate.Prelayout);
            inputField.Rebuild(CanvasUpdate.PreRender);
        }

        /// <summary>
        /// Takes a Color32 and converts it to a hex, brought to you by StackOverflow
        /// </summary>
        /// <returns>A color hex string</returns>
        private static string ColorToHex(Color32 color) {
            Color32 color32 = color;
            return String.Format("{0:X2}{1:X2}{2:X2}{3:X2}", color32.r, color32.g, color32.b, color32.a);
        }

        #endregion

        #region Printing & Output

        /// <summary>
        /// A method to act on the onEndEdit event for an InputField in Unity, checks for "Submit" event and calls <see cref="Taucon.OnInput()"/>
        /// </summary>
        private void OnEndEdit(string line)
        {
            if (Input.GetButtonDown("Submit"))
            {
                OnInput();
            }
        }

        /// <summary>
        /// Called when text has been submitted from the input field
        /// </summary>
        private void OnInput()
        {
            string input = InputField.text;

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
            Eval(input);
        }

        /// <summary>
        /// Evaluate given string (execute command)
        /// <returns> Direct output of the method that is called</returns>
        public static string Eval(string command)
        {
            string output = string.Empty;

            command.ToLower();
            Print($"{Instance.CommandOutputMarker}{command}", LogDefaultColor);

            string[] parsedCommand = command.Split(' ');
            string rawCommand = parsedCommand[0];
            string trimmedCommand = string.Join(" ", parsedCommand).Trim();

            CommandHistory.Insert(0, trimmedCommand);

            if (!Commands.ContainsKey(rawCommand))
            {
                Debug.Log($"LOGCMDNOTFOUND: {LOGCMDNOTFOUND}" +
                    $"rawCommand: {rawCommand}");
                output = LOGCMDNOTFOUND + rawCommand;
                Debug.Log(output);
                return Print(output, LogDefaultColor);
            }

            if (command == string.Empty)
            {
                return Print(String.Empty, LogDefaultColor);
            }

            LogHistory.Insert(0, command);
            if (LogHistory.Count >= Instance.MaxLines)
            {
                LogHistory.RemoveAt(LogHistory.Count - 1);

                Instance.OutputLogText.text = null;

                for (int i = LogHistory.Count - 1; i > 0; i--)
                {
                    Instance.OutputLogText.text += LogHistory[i];
                }
            }
            //_currentLogHistoryIndex = -1;

            string parameters = ExtractArguments(command, rawCommand);
            // generate output based on the method of the command
            output = Commands[rawCommand].method(parameters);

            return Print($"{Instance.LogOutputMarker}{output}", LogDefaultColor);
        }

        /// <summary>
        /// Output text to log
        /// </summary>
        public static string Print(string text, Color32 color = default(Color32))
        {
            Debug.Log(text);
            Instance.RebuildOutputUI(Instance.OutputContent, Instance.OutputViewport, Instance.Scrollbar, Instance.InputField);
            if (Instance.AddNewlineOnOutput)
            {
                return Instance.OutputLogText.text += $"<color=#{ColorToHex(color)}>{text}</color>\n";
            }
            else
            {
                return Instance.OutputLogText.text += $"<color=#{ColorToHex(color)}>{text}</color>";
            }
        }

        #endregion

        #region Initialization

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

        /// <summary>
        /// Initialize custom fonts
        /// </summary>
        private void InitCustomFonts()
        {
            if (OutputTextFont != null)
            {
                OutputLogText.font = OutputTextFont;
            }

            if (InputTextFont != null)
            {
                InputText.font = InputTextFont;
            }
        }

        /// <summary>
        /// Initialize font sizes
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
        /// Set all default log messages and their colors
        /// </summary>
        private static void InitDefaultLogMessages()
        {
            LOGCMDINVALID = $"<color=#{LogErrorColorHex}>Command invalid: </color>";
            LOGCMDNOTFOUND = $"<color=#{LogErrorColorHex}>Command unrecognized: </color>";
            LOGCMDEXIST = $"<color=#{LogErrorColorHex}>Command already exists: </color>";
        }

        /// <summary>
        /// Initialize all console options
        /// </summary>
        private void InitConsoleOptions()
        {
            InputField.characterLimit = InputCharacterLimit;
            _initialInputSelectionColor = InputField.selectionColor;
            _initialCaretColor = InputField.caretColor;
        }

        /// <summary>
        /// Initialize all GUI image color values and settings
        /// </summary>
        private void InitConsoleGUI()
        {
            InputField.caretBlinkRate = CaretBlinkRate;
            InputField.caretWidth = CaretWidth;
            InputField.customCaretColor = true;

            LogErrorColor = new Color32(239, 83, 80, 255);
            LogErrorColorHex = ColorToHex(LogErrorColor);

            switch (ColorTheme)
            {
                case PrimaryColorTheme.Dark:
                    SetConsoleColors(
                        new Color32(43, 43, 43, 255),
                        new Color32(66, 63, 62, 255),
                        new Color32(245, 244, 244, 255),
                        new Color32(245, 244, 244, 255),
                        new Color32(233, 133, 128, 255),
                        new Color32(245, 244, 244, 255));
                    LogDefaultColor = new Color32(245, 244, 244, 255);
                    LogDefaultColorHex = ColorToHex(LogDefaultColor);
                    break;
                case PrimaryColorTheme.Light:
                    SetConsoleColors(
                        new Color32(245, 244, 244, 255),
                        new Color32(225, 225, 225, 255),
                        new Color32(43, 43, 43, 255),
                        new Color32(43, 43, 43, 255),
                        new Color32(233, 133, 128, 255),
                        new Color32(43, 43, 43, 255));
                    LogDefaultColor = new Color32(43, 43, 43, 255);
                    LogDefaultColorHex = ColorToHex(LogDefaultColor);
                    break;
            }
        }

        /// <summary>
        /// Set console colours based on chosen colour variables
        /// </summary>
        private void SetConsoleColors(Color32 mainPanelColor, Color32 inputFieldColor, Color32 inputTextColor, Color32 outputTextColor, Color32 closeButtonColor, Color32 caretColor)
        {
            MainPanel.GetComponent<Image>().color = mainPanelColor;
            InputField.GetComponent<Image>().color = inputFieldColor;
            InputText.color = inputTextColor;
            CloseButton.GetComponent<Image>().color = closeButtonColor;
            InputField.caretColor = caretColor;
            OutputLogText.color = outputTextColor;
        }

        #endregion

        #region Command History

        /// <summary>
        /// Populate InputField with command history
        /// </summary>
        private void FetchHistory(KeyCode key)
        {
            Debug.Log($"Current History Index:\t{_currentLogHistoryIndex}");
            Debug.Log($"History List:\n{CommandHistory}");
            switch (key)
            {
                case KeyCode.UpArrow:
                    if (_currentLogHistoryIndex < 0)
                    {
                        _currentLogHistoryIndex++;
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
                        _currentLogHistoryIndex++;
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
                        _currentLogHistoryIndex--;
                        InputField.text = CommandHistory.ElementAt(_currentLogHistoryIndex);
                        StartCoroutine(CaretToPosition(InputField, 0));
                        break;
                    }
            }
        }

        #endregion
    }
}
