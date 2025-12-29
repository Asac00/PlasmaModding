using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visor;
using static AgentGestalt;

namespace PlasmaModding.CustomTypes
{
    public class CustomEditor<T> : DataEditor
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CustomEditor");

        public override void Setup(Agent agent, int propertyId, ProcessorUI processorUI = null, bool canClose = true)
        {
            base.Setup(agent, propertyId, processorUI, canClose);

            foreach (TMP_InputField inputField in inputFields.Values)
            {
                inputField.SetTextWithoutNotify(_runtimeProperty.GetValueText());
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    inputField.caretPosition = 0;
                }
                if (_processorUI != null)
                {
                    inputField.restoreOriginalTextOnEscape = !_runtimeProperty.definition.isScript;
                    inputField.ActivateInputField();
                }
            }
            processorUISize = editorSize;
            showApplyMessage = (_processorUI == null || !_runtimeProperty.definition.isScript);
        }

        public override void CleanUp()
        {
            base.CleanUp();
            foreach (TMP_InputField inputField in inputFields.Values)
            {
                inputField.onValueChanged.RemoveAllListeners();
                inputField.onValidateInput = null;
            }
        }

        public void HandleChange()
        {
            if (!_runtimeProperty.definition.isScript && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) && (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)))
            {
                SetData(CustomTypeManager.NewData<T>(typeName, outputValue), null);
                foreach (TMP_InputField inputField in inputFields.Values)
                {
                    inputField.DeactivateInputField(false);
                }
                EventSystem.current.SetSelectedGameObject(null);
                if (_processorUI != null)
                {
                    Apply();
                }
            }
            else
            {
                if (_processorUI != null)
                {
                    SetData(CustomTypeManager.NewData<T>(typeName, outputValue), null);
                }
            }
            SetDirty(_runtimeProperty.definition.isScript || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || (!Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter)));
        }

        public TMP_InputField GetOrCreateInputField(string name, int width, int height, int posX, int posY, int fontSize, string placeholder = "", TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
        {
            if (inputFields.ContainsKey(name))
            {
                return inputFields[name];
            }

            GameObject referenceInputFieldGO = transform.Find("InputField").gameObject;

            GameObject newInputFieldGO = GameObject.Instantiate(referenceInputFieldGO, referenceInputFieldGO.transform.parent);
            newInputFieldGO.name = name;

            RectTransform rt = newInputFieldGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_InputField newInputField = newInputFieldGO.GetComponent<TMP_InputField>();
            newInputField.textComponent.fontSize = fontSize;

            TextMeshProUGUI placeholderText = newInputField.placeholder as TextMeshProUGUI;

            // Change the placeholder text
            if (placeholderText != null)
            {
                placeholderText.text = placeholder;
                placeholderText.fontSize = fontSize;
            }

            newInputField.contentType = contentType;

            inputFields[name] = newInputField;
            
            newInputFieldGO.SetActive(true);

            return newInputField;
        }

        public TMP_Text GetOrCreateLabel(string name, int posX, int posY, string text, int fontSize)
        {
            if (labels.ContainsKey(name))
            {
                return labels[name];
            }

            GameObject referenceLabelGO = transform.Find("Label").gameObject;

            GameObject newLabelGO = GameObject.Instantiate(referenceLabelGO, referenceLabelGO.transform.parent);
            newLabelGO.name = name;

            RectTransform rt = newLabelGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_Text newLabel = newLabelGO.GetComponent<TMP_Text>();
            newLabel.fontSize = fontSize;

            newLabel.text = text;

            labels[name] = newLabel;

            newLabelGO.SetActive(true);

            return newLabel;
        }

        public TMP_Text GetOrCreateHints(string name, int posX, int posY, string text, int fontSize)
        {
            if (hints.ContainsKey(name))
            {
                return hints[name];
            }

            GameObject referenceHintGO = transform.Find("Hint").gameObject;

            GameObject newHintGO = GameObject.Instantiate(referenceHintGO, referenceHintGO.transform.parent);
            newHintGO.name = name;

            RectTransform rt = newHintGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_Text newHint = newHintGO.GetComponent<TMP_Text>();
            newHint.fontSize = fontSize;

            newHint.text = text;

            hints[name] = newHint;

            newHintGO.SetActive(true);

            return newHint;
        }

        public Toggle GetOrCreateToggle(string name, int posX, int posY, string text, int fontSize)
        {
            if (toggles.ContainsKey(name))
            {
                return toggles[name];
            }

            GameObject referenceToggleGO = transform.Find("RoundToggle").gameObject;

            GameObject newToggleGO = GameObject.Instantiate(referenceToggleGO, referenceToggleGO.transform.parent);
            newToggleGO.name = name;

            RectTransform rt = newToggleGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(posX, posY);

            Text newToggleLabel = newToggleGO.GetComponentInChildren<Text>();
            newToggleLabel.fontSize = fontSize;

            newToggleLabel.text = text;

            Toggle newToggle = newToggleGO.GetComponent<Toggle>();

            toggles[name] = newToggle;

            newToggleGO.SetActive(true);

            return newToggle;
        }

        private Dictionary<string, TMP_InputField> inputFields = new Dictionary<string, TMP_InputField>();
        private Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();
        private Dictionary<string, TMP_Text> hints = new Dictionary<string, TMP_Text>();
        private Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();

        public Vector2 editorSize;

        public string typeName;

        public T outputValue;
    }
}
