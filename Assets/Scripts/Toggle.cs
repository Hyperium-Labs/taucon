using UnityEngine;
using UnityEngine.UI;

namespace Console
{

    [AddComponentMenu("Baphomet Labs/TauCon/TauConToggle")]
    /// <summary>
    /// This script must be attached to a separate (ALWAYS ACTIVE) GameObject
    /// </summary>
    public class Toggle : MonoBehaviour
    {

        [Header("Toggle Input")]
        // Set a "Console" Axes in Project Settings > Input
        // Then set this to the Axes name value
        // Default "Console"
        // TODO(Turbits): Get the value of Input button "Console" positive button and pass it here?...might not be possible with default InputManager in Unity
        // TODO (Turbits): 2021 - added to masterplan - with the new input manager i should look to see if i can refactor this into being much easier, something like creating the input and assigning it a button programmatically.
        // i should also bring in the input here so that i can remove any added characters from the button if they get added to the input field accidentally without checking for "`" and so that i can add a custom prop to change the toggle key
        // maybe also some error checking to make sure that the button assigned isn't alphanumeric?
        public string toggleInput = "Console";
        

        private GameObject tauCon;
        private InputField inputField;

        private void Start()
        {
            inputField = Taucon.Instance.InputField;
            tauCon = Taucon.Instance.Canvas.gameObject;
            tauCon.SetActive(false);
        }

        private void Update()
        {
            if (toggleInput == string.Empty)
            {
                return;
            }

            if (Input.GetButtonDown(toggleInput))
            {
                tauCon.SetActive(!tauCon.activeSelf);
                // Remove any added characters from the toggleCommand string
                if (tauCon.activeSelf)
                {
                    if (inputField.text.EndsWith("`"))
                    {
                        inputField.text = inputField.text[inputField.text.Length-1].ToString().Replace("`", "");
                    }
                }

                if (!Taucon.Instance.RefocusConsoleOnSubmit)
                {
                    StartCoroutine(Taucon.CaretToPosition(inputField, inputField.text.Length));
                }

                Taucon.Instance.InputField.Select();
                Taucon.Instance.InputField.ActivateInputField();
            }
        }
    }
}
