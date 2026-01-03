using BepInEx.Logging;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Visor;

namespace PlasmaModding.CustomTypes
{
    public class CustomEditor<T> : DataEditor
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CustomEditor");

        public virtual void BuildUI()
        {
            return;
        }

        public virtual void ApplyValueToUI(T value)
        {
            return;
        }

        public void AddListener<TValue>(Action<TValue> callback, UnityEvent<TValue> @event)
        {
            @event.AddListener(value =>
            {
                callback(value);

                if(correctOutputValue)
                {
                    // Push the output value
                    SetData(CustomTypeManager.NewData<T>(typeName, outputValue), null);
                }

                applyButton.interactable = correctOutputValue;

                SetDirty(correctOutputValue);
            });
        }

        public override void Setup(Agent agent, int propertyId, ProcessorUI processorUI = null, bool canClose = true)
        {
            BuildUI();

            base.Setup(agent, propertyId, processorUI, canClose);

            foreach (TMP_InputField inputField in inputFields.Values)
            {
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    inputField.caretPosition = 0;
                }
                if (_processorUI != null)
                {
                    inputField.ActivateInputField();
                }
            }

            processorUISize = editorSize;
            showApplyMessage = (_processorUI == null || !_runtimeProperty.definition.isScript);

            applyButton.navigation = new Navigation { mode = Navigation.Mode.None };

            outputValue = (T)CustomTypeManager.customTypesProperties[typeName].defaultValue;

            if(_runtimeProperty != null)
            {
                outputValue = CustomTypeManager.GetValueCustomType<T>(_runtimeProperty, typeName);
            }

            ApplyValueToUI(outputValue);

            SetData(CustomTypeManager.NewData<T>(typeName, outputValue), null);

            applyButton.onClick.AddListener(() => Validate());

            SetDirty(true);
        }

        public override void CleanUp()
        {
            base.CleanUp();
            foreach (TMP_InputField inputField in inputFields.Values)
            {
                inputField.onValueChanged.RemoveAllListeners();
                inputField.onValidateInput = null;
            }
            foreach (Toggle toggle in toggles.Values)
            {
                toggle.onValueChanged.RemoveAllListeners();
            }
        }

        protected virtual void Update()
        {
            if (!isDirty)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Validate(true);
            }
        }

        public void Validate(bool callOnClick = false)
        {
            if (isDirty)
            {
                // Deactivate input fields
                foreach (TMP_InputField inputField in inputFields.Values)
                {
                    inputField.DeactivateInputField(false);
                }

                applyButton.onClick.RemoveAllListeners();

                if (_processorUI != null && callOnClick)
                {
                    Apply();
                }

                // Clean state
                SetDirty(false);

                // Unfocus everything
                EventSystem.current.SetSelectedGameObject(null);
            }
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

            newInputField.onSubmit.RemoveAllListeners(); // Remove the action of Enter on the input field

            inputFields[name] = newInputField;
            
            newInputFieldGO.SetActive(true);

            return newInputField;
        }

        public void ChangeInputFieldContent(string name, string content)
        {
            inputFields[name].SetTextWithoutNotify(content);
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

        public void ChangeLabelText(string name, string text)
        {
            labels[name].text = text;
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

        public void ChangeHintText(string name,  string text)
        {
            hints[name].text = text;
        }

        public Toggle GetOrCreateToggle(string name, int posX, int posY, string label, int fontSize)
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

            newToggleLabel.text = label;

            Toggle newToggle = newToggleGO.GetComponent<Toggle>();

            newToggle.navigation = new Navigation { mode = Navigation.Mode.None }; // Remove the action of Enter on the input field

            toggles[name] = newToggle;

            newToggleGO.SetActive(true);

            return newToggle;
        }

        public void ChangeToggleLabel(string name, string label)
        {
            Text toggleLabel = toggles[name].GetComponentInChildren<Text>();
            toggleLabel.text = label;
        }

        public void ChangeToggleState(string name, bool isOn)
        {
            toggles[name].SetIsOnWithoutNotify(isOn);
        }

        public Image GetOrCreateImage(string name, int width, int height, int posX, int posY, Sprite sprite, Color color)
        {
            if (images.ContainsKey(name))
            {
                return images[name];
            }

            GameObject referenceImageGO = transform.Find("Image").gameObject;

            GameObject newImageGO = GameObject.Instantiate(referenceImageGO, referenceImageGO.transform.parent);
            newImageGO.name = name;

            RectTransform rt = newImageGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(posX, posY);

            Image newImage = newImageGO.GetComponent<Image>();
            newImage.sprite = sprite;
            newImage.color = color;

            images[name] = newImage;

            newImageGO.SetActive(true);

            return newImage;
        }

        public void ChangeImageSprite(string name, Sprite sprite)
        {
            images[name].sprite = sprite;
        }

        public void ChangeImageColor(string name, Color color)
        {
            images[name].color = color;
        }

        // A height of 20 is recommended 
        public Slider GetOrCreateSlider(string name, int width, int height, int posX, int posY, string label, int fontSize)
        {
            if (sliders.ContainsKey(name))
            {
                return sliders[name];
            }

            GameObject referenceSliderGO = transform.Find("Slider").gameObject;

            GameObject newSliderGO = GameObject.Instantiate(referenceSliderGO, referenceSliderGO.transform.parent);
            newSliderGO.name = name;

            RectTransform rt = newSliderGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(posX, posY);

            Text newSliderLabel = newSliderGO.GetComponentInChildren<Text>();
            newSliderLabel.fontSize = fontSize;

            newSliderLabel.text = label;

            Slider newSlider = newSliderGO.GetComponent<Slider>();

            sliders[name] = newSlider;

            newSliderGO.SetActive(true);

            return newSlider;
        }

        public void ChangeSliderLabel(string name, string label)
        {
            Text sliderLabel = sliders[name].GetComponentInChildren<Text>();
            sliderLabel.text = label;
        }

        private Dictionary<string, TMP_InputField> inputFields = new Dictionary<string, TMP_InputField>();

        private Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();

        private Dictionary<string, TMP_Text> hints = new Dictionary<string, TMP_Text>();

        private Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();

        private Dictionary<string, Image> images = new Dictionary<string, Image>();

        private Dictionary<string, Slider> sliders = new Dictionary<string, Slider>();

        public Vector2 editorSize;

        public string typeName;

        public T outputValue;

        public bool correctOutputValue = true;
    }
}
