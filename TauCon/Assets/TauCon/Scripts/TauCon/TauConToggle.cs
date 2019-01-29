using UnityEngine;
using UnityEngine.UI;

namespace TauConsole
{

    [AddComponentMenu("Scripts/TauCon/TauConToggle")]
    /// <summary>
    /// This script must be attached to a separate (always active) GameObject
    /// </summary>
    public class TauConToggle : MonoBehaviour
    {

        [Header("Toggle Button")]
        // Set a "Console" Axes in Project Settings > Input
        // Then set this to the Axes name value
        // Default "Console"
        public string toggleCommand = "Console";
        // TODO(Trevor Woodman): Get the value of Input button "Console" positive button and pass it here...might not be possible with default InputManager in Unity

        private GameObject tauCon;
        private InputField inputField;

        /// <summary>
        /// Called once in the lifetime of a script, after all Awake functions on all objects in a scene are called.
        /// </summary>
        private void Start()
        {
            inputField = TauCon.Instance.InputField;
            tauCon = TauCon.Instance.TauConCanvas.gameObject;
            tauCon.SetActive(false);
        }

        /// <summary>
        /// Called every frame, but update interval times will vary depending on FPS.
        /// </summary>
        void Update()
        {
            if (toggleCommand == string.Empty)
            {
                return;
            }

            if (Input.GetButtonDown(toggleCommand))
            {
                tauCon.SetActive(!tauCon.activeSelf);
                // Remove any added characters from the toggleCommand string
                if (tauCon.activeSelf)
                {
                    if (inputField.text.Contains("`"))
                    {
                        inputField.text = inputField.text.Replace("`", "");
                    }
                }

                if (!TauCon.Instance.ReselectOnSubmit)
                {
                    StartCoroutine(TauCon.CaretToEnd(inputField));
                }
            }
        }
    }
}
