using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TauConsole
{

    [AddComponentMenu("Scripts/TauCon/TauCon")]
    /// <summary>
    /// This script must be attached to the main TauCon Canvas
    /// Default UI Element Name: TauCon
    /// </summary>
    public class TauCon : MonoBehaviour
    {

        #region Theme Enums

        public enum PrimaryColorTheme
        {
            Default,
            Dark,
            Light
        }

        public enum SecondaryColorTheme
        {
            Default,
            Dark,
            Light,
            CandyApple,
            Crimson,
            Vermillion,
            Saffron,
            Lemon,
            Alien,
            Avocado,
            Emerald,
            Cerulean,
            Cobalt,
            Electric,
            Blizzard,
            Lilac,
            Mauve,
            Eminence,
            CottonCandy,
            Rogue
        }

        #endregion

        #region User Variables

        [Header("UI Components")]
        public Canvas TauConCanvas;
        public GameObject VersionPanel;
        public Text VersionText;
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
        public Button ResetPositionButton;
        public Button CloseConsoleButton;
        public Button ResizePanelButton;

        [Header("Console Options")]
        public char PromptSymbol = '>';
        public string ConsoleVersionText = "TauCon";
        public PrimaryColorTheme PredefinedPrimaryColorTheme;
        public SecondaryColorTheme PredefinedSecondaryColorTheme;
        [Tooltip("This means you are going to set your OWN colours, NOT use one of the predefined themes.")]
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
        public bool AllowEmptyOutput = false;
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

        [Header("Default GUI Colors")]
        public Color32 VersionPanelBackgroundRGBA;
        public Color32 VersionTextRGBA;
        public Color32 OutputPanelBackgroundRGBA;
        public Color32 InputFieldBackgroundRGBA;
        public Color32 InputTextRGBA;
        public Color32 InputPlaceholderTextRGBA;
        public Color32 InputCaretColorRGBA;
        public Color32 InputSelectionColorRGBA;
        public Color32 ScrollbarBackgroundRGBA;
        public Color32 ScrollbarHandleRGBA;
        public Color32 ResetPositionButtonColorRGBA;
        public Color32 CloseConsoleButtonColorRGBA;
        public Color32 ResizePanelButtonColorRGBA;

        [Header("Log Colors")]
        public Color32 CommandColorRGBA = new Color32(131, 212, 179, 255);
        public Color32 LogColorRGBA = new Color32(188, 186, 184, 255);
        public Color32 AssertColorRGBA = new Color32(214, 200, 255, 255);
        public Color32 WarningColorRGBA = new Color32(254, 206, 168, 255);
        public Color32 ErrorColorRGBA = new Color32(255, 132, 123, 255);
        public Color32 ExceptionColorRGBA = new Color32(232, 74, 95, 255);
        public Color32 ParamColorRGBA = new Color32(163, 222, 131, 255);
        public Color32 HelpColorRGBA = new Color32(157, 229, 255, 255);
        public Color32 HelpListColorRGBA = new Color32(213, 253, 255, 255);

        #endregion

        // Vars to store hex of RGBA
        public static string CommandColor;
        public static string LogColor;
        public static string AssertColor;
        public static string WarningColor;
        public static string ErrorColor;
        public static string ExceptionColor;
        public static string ParamColor;
        public static string HelpColor;
        public static string HelpListColor;

        private static Color32 _initialInputSelectionColor;
        private static Color32 _initialCaretColor;
        private static int _currentIndex;
#pragma warning disable
        private float _outputContentHeight;
#pragma warning enable

        private Vector2 _outputContentReset = new Vector2(0f, 0f);

        public static TauCon Instance;
        public static Dictionary<string, TauConCommand> Commands = new Dictionary<string, TauConCommand>();
        public static List<string> CommandHistory = new List<string>();
        public static List<string> LogHistory = new List<string>();
        public delegate void ConsoleListener(string line);
        public static event ConsoleListener OnOutputEvent;

        static TauCon() { }

        #region Unity Callbacks

        /// <summary>
        /// Called once in the lifetime of a script, before any Start functions are called.
        /// </summary>
        private void Awake()
        {
            // If a console instance doesn't already exist
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            // Init Commands Dictionary
            Commands = new Dictionary<string, TauConCommand>();
        }

        /// <summary>
        /// Called once in the lifetime of a script, after all Awake functions on all objects in a scene are called.
        /// </summary>
        private void Start()
        {
            OnOutputEvent += OnOutput;
            // If Unity log output is enabled
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
            InitLogColors();
            InitDefaultLogMessages();
            InitDefaultCommands();

            InputField.onEndEdit.AddListener(OnEndEdit);
            _outputContentHeight = OutputContent.rect.height;

            // Initialize OutputLog as empty string (remove test text from Editor)
            OutputLogText.text = string.Empty;
        }

        /// <summary>
        /// Called every frame, but update interval times will vary depending on FPS.
        /// </summary>
        private void Update()
        {
            // Check for active console and 'return' event for command input
            if (TauConCanvas.gameObject.activeInHierarchy)
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

            // TODO(Trevor Woodman): TESTING, REMOVE
            if (!UseCustomTheme)
            {
                switch (PredefinedPrimaryColorTheme)
                {
                    case PrimaryColorTheme.Default:
                        SetPrimaryColors(
                            new Color32(46, 46, 46, 255),
                            new Color32(58, 58, 58, 255),
                            new Color32(73, 73, 73, 255),
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255),
                            new Color32(85, 85, 85, 255));
                        break;
                    case PrimaryColorTheme.Dark:
                        SetPrimaryColors(
                            new Color32(46, 46, 46, 255),
                            new Color32(58, 58, 58, 255),
                            new Color32(73, 73, 73, 255),
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255),
                            new Color32(85, 85, 85, 255));
                        break;
                    case PrimaryColorTheme.Light:
                        SetPrimaryColors(
                            new Color32(158, 158, 158, 255),
                            new Color32(224, 224, 224, 255),
                            new Color32(238, 238, 238, 255),
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255),
                            new Color32(85, 85, 85, 255));
                        break;
                }

                switch (PredefinedSecondaryColorTheme)
                {
                    case SecondaryColorTheme.Default:
                        // 83d4b3
                        // 131, 212, 179
                        SetHighlightColors(
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255));
                        break;
                    case SecondaryColorTheme.Dark:
                        // 757575
                        // 117, 117, 117
                        SetHighlightColors(
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255));
                        break;
                    case SecondaryColorTheme.Light:
                        // F5F5F5
                        // 245, 245, 245
                        SetHighlightColors(
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(164, 164, 164, 255));
                        break;
                    case SecondaryColorTheme.CandyApple:
                        // e51b13
                        // 229, 27, 19
                        SetHighlightColors(
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255));
                        break;
                    case SecondaryColorTheme.Crimson:
                        // 771114
                        // 119, 17, 20
                        SetHighlightColors(
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255));
                        break;
                    case SecondaryColorTheme.Vermillion:
                        // f35e62
                        // 243, 94, 98
                        SetHighlightColors(
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255));
                        break;
                    case SecondaryColorTheme.Saffron:
                        // e19409
                        // 225, 148, 9
                        SetHighlightColors(
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255));
                        break;
                    case SecondaryColorTheme.Lemon:
                        // e9e230
                        // 233, 226, 48
                        SetHighlightColors(
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255));
                        break;
                    case SecondaryColorTheme.Alien:
                        // 9de717
                        // 157, 231, 23
                        SetHighlightColors(
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255));
                        break;
                    case SecondaryColorTheme.Avocado:
                        // 40b540
                        // 64, 181, 64
                        SetHighlightColors(
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255));
                        break;
                    case SecondaryColorTheme.Emerald:
                        // 18ed7c
                        // 24, 237, 124
                        SetHighlightColors(
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255));
                        break;
                    case SecondaryColorTheme.Cerulean:
                        // 05afed
                        // 5, 175, 237
                        SetHighlightColors(
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255));
                        break;
                    case SecondaryColorTheme.Cobalt:
                        // 166bf3
                        // 22, 107, 243
                        SetHighlightColors(
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255));
                        break;
                    case SecondaryColorTheme.Electric:
                        // 09e6e7
                        // 9, 230, 231
                        SetHighlightColors(
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255));
                        break;
                    case SecondaryColorTheme.Blizzard:
                        // 90cfe4
                        // 144, 207, 228
                        SetHighlightColors(
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255));
                        break;
                    case SecondaryColorTheme.Lilac:
                        // ddafe7
                        // 221, 175, 231
                        SetHighlightColors(
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255));
                        break;
                    case SecondaryColorTheme.Mauve:
                        // a74c89
                        // 167, 76, 137
                        SetHighlightColors(
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255));
                        break;
                    case SecondaryColorTheme.Eminence:
                        // b355d7
                        // 179, 85, 215
                        SetHighlightColors(
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255));
                        break;
                    case SecondaryColorTheme.CottonCandy:
                        // f3b0db
                        // 243, 176, 219
                        SetHighlightColors(
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255));
                        break;
                    case SecondaryColorTheme.Rogue:
                        // e534a1
                        // 229, 52, 161
                        SetHighlightColors(
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255));
                        break;
                }
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

            output += logString + (Instance.OutputStackTrace ? "\n" + Colorify(trace, LogColor) : String.Empty);
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

            // Add the command to the dictionary
            TauConCommand consoleCommand = new TauConCommand(name, command, description, method, helpText);
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

            //// Print command
            Print(Instance.PromptSymbol + " " + Colorify(command, CommandColor));

            if (string.IsNullOrEmpty(command))
            {
                Debug.Log("Invalid Cmd");
                output = LOGINVALIDCMD;
                return Print(output);
            }

            command.ToLower();

            string[] parsedCmd = command.Split(' ');

            // Raw command without args
            string rawCmd = parsedCmd[0];

            // Remove any extra spaces and store for History use
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

            // DEV NOTE:
            // The reason I have decided to add this check *after* the History logic
            // is so that the History acts exactly like bash. Even if the past command
            // or argument is invalid it will add it regardless. 
            // This is a personal preference thing.

            // If the command is not found in the Commands Dictionary
            if (!Commands.ContainsKey(rawCmd))
            {
                Debug.Log("Command not found");
                output = LOGCMDNOTFOUND + rawCmd;
                return Print(output);
            }

            // Call the ExtractParameters method and store the parameters in a variable
            string parameters = ExtractParameters(command, rawCmd);
            // Combine command and parameters
            output = Commands[rawCmd].method(parameters);

            // Newline check
            if (Instance.NewlineOnOutput)
            {
                output += "\n";
            }

            // Reset currentIndex
            _currentIndex = -1;

            // Return the output (print to the output log)
            return Print(output);
        }

        #endregion

        #region Utility Methods

        /// <summary>Used to color text in the logger by wrapping text in color tags
        /// <para>string text</para>
        /// <para>[string color(hex) = null]</para>
        /// </summary>
        public static string Colorify(string text, string color = null)
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

            AddCommand("Quit", "quit", "Quits the application.", CommandQuit.QuitApplication, "noargs");

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
                VersionText.font = VersionTextFont;
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
                VersionText.fontSize = VersionTextFontSize;
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
            LOGINVALIDCMD = Colorify("Command invalid: ", ExceptionColor);
            LOGCMDNOTFOUND = Colorify("Command unrecognized: ", ExceptionColor);
            LOGERROR = Colorify("Error: ", ErrorColor);
            LOGWARNING = Colorify("Warning: ", WarningColor);
            LOGDEFAULT = Colorify("Log: ", LogColor);
            LOGEXCEPTION = Colorify("Exception: ", ExceptionColor);
            LOGASSERT = Colorify("Assert: ", AssertColor);
        }

        /// <summary>
        /// Initialize all log colors in Hex from the given RGBA colors set by default or in the editor.
        /// </summary>
        private void InitLogColors()
        {
            CommandColor = ColorUtility.ToHtmlStringRGBA(CommandColorRGBA);
            LogColor = ColorUtility.ToHtmlStringRGBA(LogColorRGBA);
            AssertColor = ColorUtility.ToHtmlStringRGBA(AssertColorRGBA);
            WarningColor = ColorUtility.ToHtmlStringRGBA(WarningColorRGBA);
            ErrorColor = ColorUtility.ToHtmlStringRGBA(ErrorColorRGBA);
            ExceptionColor = ColorUtility.ToHtmlStringRGBA(ExceptionColorRGBA);
            ParamColor = ColorUtility.ToHtmlStringRGBA(ParamColorRGBA);
            HelpColor = ColorUtility.ToHtmlStringRGBA(HelpColorRGBA);
            HelpListColor = ColorUtility.ToHtmlStringRGBA(HelpListColorRGBA);
        }

        /// <summary>
        /// Initialize all console options.
        /// </summary>
        private void InitConsoleOptions()
        {
            InputField.characterLimit = InputCharacterLimit;
            _initialInputSelectionColor = InputField.selectionColor;
            _initialCaretColor = InputField.caretColor;
            VersionText.text = ConsoleVersionText;
        }

        /// <summary>
        /// Sets all GUI image color values and settings.
        /// </summary>
        private void InitConsoleGUI()
        {
            if (!UseCustomTheme)
            {
                switch (PredefinedPrimaryColorTheme)
                {
                    case PrimaryColorTheme.Default:
                        SetPrimaryColors(
                            new Color32(46, 46, 46, 255), 
                            new Color32(58, 58, 58, 255), 
                            new Color32(73, 73, 73, 255), 
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255), 
                            new Color32(85, 85, 85, 255));
                        break;
                    case PrimaryColorTheme.Dark:
                        SetPrimaryColors(
                            new Color32(46, 46, 46, 255),
                            new Color32(58, 58, 58, 255),
                            new Color32(73, 73, 73, 255),
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255),
                            new Color32(85, 85, 85, 255));
                        break;
                    case PrimaryColorTheme.Light:
                        SetPrimaryColors(
                            new Color32(158, 158, 158, 255),
                            new Color32(224, 224, 224, 255),
                            new Color32(238, 238, 238, 255),
                            new Color32(188, 186, 184, 255),
                            new Color32(164, 164, 164, 255),
                            new Color32(85, 85, 85, 255));
                        break;
                }

                switch (PredefinedSecondaryColorTheme)
                {
                    case SecondaryColorTheme.Default:
                        // 83d4b3
                        // 131, 212, 179
                        SetHighlightColors(
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255),
                            new Color32(131, 212, 179, 255));
                        break;
                    case SecondaryColorTheme.Dark:
                        // 757575
                        // 117, 117, 117
                        SetHighlightColors(
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255),
                            new Color32(117, 117, 117, 255));
                        break;
                    case SecondaryColorTheme.Light:
                        // F5F5F5
                        // 245, 245, 245
                        SetHighlightColors(
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(245, 245, 245, 255),
                            new Color32(164, 164, 164, 255));
                        break;
                    case SecondaryColorTheme.CandyApple:
                        // e51b13
                        // 229, 27, 19
                        SetHighlightColors(
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255),
                            new Color32(229, 27, 19, 255));
                        break;
                    case SecondaryColorTheme.Crimson:
                        // 771114
                        // 119, 17, 20
                        SetHighlightColors(
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255),
                            new Color32(119, 17, 20, 255));
                        break;
                    case SecondaryColorTheme.Vermillion:
                        // f35e62
                        // 243, 94, 98
                        SetHighlightColors(
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255),
                            new Color32(243, 94, 98, 255));
                        break;
                    case SecondaryColorTheme.Saffron:
                        // e19409
                        // 225, 148, 9
                        SetHighlightColors(
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255),
                            new Color32(225, 148, 9, 255));
                        break;
                    case SecondaryColorTheme.Lemon:
                        // e9e230
                        // 233, 226, 48
                        SetHighlightColors(
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255),
                            new Color32(233, 226, 48, 255));
                        break;
                    case SecondaryColorTheme.Alien:
                        // 9de717
                        // 157, 231, 23
                        SetHighlightColors(
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255),
                            new Color32(157, 231, 23, 255));
                        break;
                    case SecondaryColorTheme.Avocado:
                        // 40b540
                        // 64, 181, 64
                        SetHighlightColors(
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255),
                            new Color32(64, 181, 64, 255));
                        break;
                    case SecondaryColorTheme.Emerald:
                        // 18ed7c
                        // 24, 237, 124
                        SetHighlightColors(
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255),
                            new Color32(24, 237, 124, 255));
                        break;
                    case SecondaryColorTheme.Cerulean:
                        // 05afed
                        // 5, 175, 237
                        SetHighlightColors(
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255),
                            new Color32(5, 175, 237, 255));
                        break;
                    case SecondaryColorTheme.Cobalt:
                        // 166bf3
                        // 22, 107, 243
                        SetHighlightColors(
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255),
                            new Color32(22, 107, 243, 255));
                        break;
                    case SecondaryColorTheme.Electric:
                        // 09e6e7
                        // 9, 230, 231
                        SetHighlightColors(
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255),
                            new Color32(9, 230, 231, 255));
                        break;
                    case SecondaryColorTheme.Blizzard:
                        // 90cfe4
                        // 144, 207, 228
                        SetHighlightColors(
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255),
                            new Color32(144, 207, 228, 255));
                        break;
                    case SecondaryColorTheme.Lilac:
                        // ddafe7
                        // 221, 175, 231
                        SetHighlightColors(
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255),
                            new Color32(221, 175, 231, 255));
                        break;
                    case SecondaryColorTheme.Mauve:
                        // a74c89
                        // 167, 76, 137
                        SetHighlightColors(
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255),
                            new Color32(167, 76, 137, 255));
                        break;
                    case SecondaryColorTheme.Eminence:
                        // b355d7
                        // 179, 85, 215
                        SetHighlightColors(
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255),
                            new Color32(179, 85, 215, 255));
                        break;
                    case SecondaryColorTheme.CottonCandy:
                        // f3b0db
                        // 243, 176, 219
                        SetHighlightColors(
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255),
                            new Color32(243, 176, 219, 255));
                        break;
                    case SecondaryColorTheme.Rogue:
                        // e534a1
                        // 229, 52, 161
                        SetHighlightColors(
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255),
                            new Color32(229, 52, 161, 255));
                        break;
                }
            }

            // Options
            InputField.caretBlinkRate = CaretBlinkRate;
            InputField.caretWidth = CaretWidth;
            InputField.customCaretColor = CaretCustomColor;
        }

        private void SetPrimaryColors(Color32 colorVersionPanel, Color32 colorOutputPanel, Color32 colorInputField, Color32 colorInputText, Color32 colorInputPlaceholderText, Color32 colorScrollbar)
        {
            VersionPanel.GetComponent<Image>().color = new Color32(colorVersionPanel.r, colorVersionPanel.g, colorVersionPanel.b, 255);
            OutputPanel.GetComponent<Image>().color = new Color32(colorOutputPanel.r, colorOutputPanel.g, colorOutputPanel.b, 255);
            InputField.GetComponent<Image>().color = new Color32(colorInputField.r, colorInputField.g, colorInputField.b, 255);
            InputText.color = new Color32(colorInputText.r, colorInputText.g, colorInputText.b, 255);
            InputPlaceholderText.color = new Color32(colorInputPlaceholderText.r, colorInputPlaceholderText.g, colorInputPlaceholderText.b, 255);
            Scrollbar.GetComponent<Image>().color = new Color32(colorScrollbar.r, colorScrollbar.g, colorScrollbar.b, 255);
        }

        private void SetHighlightColors(Color32 colorVersionText, Color32 colorCaret, Color32 colorSelection, Color32 colorScrollbarHandle, Color32 colorResetPositionButton, Color32 colorCloseConsoleButton, Color32 colorResizePanelButton)
        {
            VersionText.color = new Color32(colorVersionText.r, colorVersionText.g, colorVersionText.b, 255);
            InputField.caretColor = new Color32(colorCaret.r, colorCaret.g, colorCaret.b, 255);
            InputField.selectionColor = new Color32(colorSelection.r, colorSelection.g, colorSelection.b, 125);
            ScrollbarHandle.GetComponent<Image>().color = new Color32(colorScrollbarHandle.r, colorScrollbarHandle.g, colorScrollbarHandle.b, 200);
            ResetPositionButton.GetComponent<Image>().color = new Color32(colorResetPositionButton.r, colorResetPositionButton.g, colorResetPositionButton.b, 200);
            CloseConsoleButton.GetComponent<Image>().color = new Color32(colorCloseConsoleButton.r, colorCloseConsoleButton.g, colorCloseConsoleButton.b, 200);
            ResizePanelButton.GetComponent<Image>().color = new Color32(colorResizePanelButton.r, colorResizePanelButton.g, colorResizePanelButton.b, 200);
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
