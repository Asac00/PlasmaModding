using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public override void Setup(Agent agent, int propertyId, ProcessorUI processorUI = null, bool canClose = true)
        {
            background = transform.parent.parent.gameObject;

            RectTransform bg = background.GetComponent<RectTransform>();
            Logger.LogWarning(bg.sizeDelta.x);
            Logger.LogWarning(bg.sizeDelta.y);

            BuildUI();

            base.Setup(agent, propertyId, processorUI, canClose);

            // TODO : This has not to be here
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

            outputValue = (T)CustomTypeManager.customTypesProperties[typeName].defaultValue;

            if (_runtimeProperty != null)
            {
                outputValue = CustomTypeManager.GetValueCustomType<T>(_runtimeProperty, typeName);
            }

            ApplyValueToUI(outputValue);

            WriteData();

            if (applyButton != null)
            {
                applyButton.navigation = new Navigation { mode = Navigation.Mode.None };
                applyButton.onClick.AddListener(() => Validate());
            }

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
            foreach (Slider slider in sliders.Values)
            {
                slider.onValueChanged.RemoveAllListeners();
            }
            foreach (Button button in buttons.Values)
            {
                button.onClick.RemoveAllListeners();
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

        public void Validate(bool apply = false)
        {
            if (isDirty)
            {
                WriteData();

                // Deactivate input fields
                foreach (TMP_InputField inputField in inputFields.Values)
                {
                    inputField.DeactivateInputField(false);
                }

                if (applyButton != null)
                {
                    applyButton.onClick.RemoveAllListeners();
                }

                if (_processorUI != null && apply)
                {
                    Apply();
                }

                // Clean state
                SetDirty(false);

                // Unfocus everything
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void WriteData()
        {
            SetData(CustomTypeManager.NewData<T>(typeName, outputValue), null);
        }

        //
        // Creation of UI elements
        //

        public TMP_InputField GetOrCreateInputField(string name, int width, int height, int posX = 0, int posY = 0, int fontSize = 56, string placeholder = "", TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.InputField)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return inputFields[name];
            }

            GameObject referenceInputFieldGO = transform.Find("InputField").gameObject;

            GameObject newInputFieldGO = Instantiate(referenceInputFieldGO, transform);
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
            uiElements[name] = UI.InputField;
            rects[name] = rt;

            newInputFieldGO.SetActive(true);

            return newInputField;
        }

        public void ChangeInputFieldContent(string name, string content)
        {
            if (!inputFields.ContainsKey(name))
            {
                Logger.LogError("There's no input field with the name : " + name);
                return;
            }
            inputFields[name].text = content;
        }

        public TMP_Text GetOrCreateLabel(string name, string text, int posX = 0, int posY = 0, int fontSize = 42)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.Label)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return labels[name];
            }

            GameObject referenceLabelGO = transform.Find("Label").gameObject;

            GameObject newLabelGO = Instantiate(referenceLabelGO, transform);
            newLabelGO.name = name;

            RectTransform rt = newLabelGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_Text newLabel = newLabelGO.GetComponent<TMP_Text>();
            newLabel.fontSize = fontSize;

            newLabel.text = text;

            labels[name] = newLabel;
            uiElements[name] = UI.Label;
            rects[name] = rt;

            newLabelGO.SetActive(true);

            return newLabel;
        }

        public void ChangeLabelText(string name, string text)
        {
            if (!labels.ContainsKey(name))
            {
                Logger.LogError("There's no label with the name : " + name);
                return;
            }
            labels[name].text = text;
        }

        public TMP_Text GetOrCreateHints(string name, string text, int posX = 0, int posY = 0, int fontSize = 35)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.Hint)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return hints[name];
            }

            GameObject referenceHintGO = transform.Find("Hint").gameObject;

            GameObject newHintGO = Instantiate(referenceHintGO, transform);
            newHintGO.name = name;

            RectTransform rt = newHintGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_Text newHint = newHintGO.GetComponent<TMP_Text>();
            newHint.fontSize = fontSize;

            newHint.text = text;

            hints[name] = newHint;
            uiElements[name] = UI.Hint;
            rects[name] = rt;

            newHintGO.SetActive(true);

            return newHint;
        }

        public void ChangeHintText(string name,  string text)
        {
            if (!hints.ContainsKey(name))
            {
                Logger.LogError("There's no hint with the name : " + name);
                return;
            }
            hints[name].text = text;
        }

        public Toggle GetOrCreateToggle(string name, string label, int posX, int posY, int fontSize = 42)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.Toggle)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return toggles[name];
            }

            GameObject referenceToggleGO = transform.Find("RoundToggle").gameObject;

            GameObject newToggleGO = Instantiate(referenceToggleGO, transform);
            newToggleGO.name = name;

            RectTransform rt = newToggleGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(posX, posY);

            Text newToggleLabel = newToggleGO.GetComponentInChildren<Text>();
            newToggleLabel.fontSize = fontSize;

            newToggleLabel.text = label;

            Toggle newToggle = newToggleGO.GetComponent<Toggle>();

            newToggle.navigation = new Navigation { mode = Navigation.Mode.None }; // Remove the action of Enter on the toggle

            toggles[name] = newToggle;
            uiElements[name] = UI.Toggle;
            rects[name] = rt;

            newToggleGO.SetActive(true);

            return newToggle;
        }

        public void ChangeToggleLabel(string name, string label)
        {
            if (!toggles.ContainsKey(name))
            {
                Logger.LogError("There's no toggle with the name : " + name);
                return;
            }
            Text toggleLabel = toggles[name].GetComponentInChildren<Text>();
            toggleLabel.text = label;
        }

        public void ChangeToggleState(string name, bool isOn)
        {
            if (!toggles.ContainsKey(name))
            {
                Logger.LogError("There's no toggle with the name : " + name);
                return;
            }
            toggles[name].gameObject.SetActive(false);
            toggles[name].isOn = isOn;
            toggles[name].gameObject.SetActive(true);
        }

        public Image GetOrCreateImage(string name, Sprite sprite, Color color, int width, int height, int posX = 0, int posY = 0)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.Image)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return images[name];
            }

            GameObject referenceImageGO = transform.Find("Image").gameObject;

            GameObject newImageGO = Instantiate(referenceImageGO, transform);
            newImageGO.name = name;

            RectTransform rt = newImageGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(posX, posY);

            Image newImage = newImageGO.GetComponent<Image>();
            newImage.sprite = sprite;
            newImage.color = color;

            images[name] = newImage;
            uiElements[name] = UI.Image;
            rects[name] = rt;

            newImageGO.SetActive(true);

            return newImage;
        }

        public void ChangeImageSprite(string name, Sprite sprite)
        {
            if (!images.ContainsKey(name))
            {
                Logger.LogError("There's no image with the name : " + name);
                return;
            }
            images[name].sprite = sprite;
        }

        public void ChangeImageColor(string name, Color color)
        {
            if (!images.ContainsKey(name))
            {
                Logger.LogError("There's no image with the name : " + name);
                return;
            }
            images[name].color = color;
        }

        // A height of 20 is recommended 
        public Slider GetOrCreateSlider(string name, string label, int width, int height, int posX = 0, int posY = 0, int fontSize = 35)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.Slider)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return sliders[name];
            }

            GameObject referenceSliderGO = transform.Find("Slider").gameObject;

            GameObject newSliderGO = Instantiate(referenceSliderGO, transform);
            newSliderGO.name = name;

            RectTransform rt = newSliderGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_Text newSliderLabel = newSliderGO.GetComponentInChildren<TMP_Text>();
            newSliderLabel.fontSize = fontSize;

            newSliderLabel.text = label;

            Slider newSlider = newSliderGO.GetComponent<Slider>();

            sliders[name] = newSlider;
            uiElements[name] = UI.Slider;
            rects[name] = rt;

            newSliderGO.SetActive(true);

            return newSlider;
        }

        // The slider value must be between 0 and 1
        public void ChangeSliderValue(string name, float value)
        {
            if (!sliders.ContainsKey(name))
            {
                Logger.LogError("There's no slider with the name : " + name);
                return;
            }
            if (value < 0 || value > 1)
            {
                Logger.LogError("Slider value is out of range !");
                return;
            }
            sliders[name].value = value;
        }

        public void ChangeSliderLabel(string name, string label)
        {
            if (!sliders.ContainsKey(name))
            {
                Logger.LogError("There's no slider with the name : " + name);
                return;
            }
            TMP_Text sliderLabel = sliders[name].GetComponentInChildren<TMP_Text>();
            sliderLabel.text = label;
        }

        public Button GetOrCreateButton(string name, string label, int width, int height, int posX = 0, int posY = 0, int fontSize = 42)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.Button)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return buttons[name];
            }

            GameObject referenceButtonGO = transform.Find("Button").gameObject;

            GameObject newButtonGO = Instantiate(referenceButtonGO, transform);
            newButtonGO.name = name;

            RectTransform rt = newButtonGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_Text newButtonLabel = newButtonGO.GetComponentInChildren<TMP_Text>();
            newButtonLabel.fontSize = fontSize;

            newButtonLabel.text = label;

            Button newButton = newButtonGO.GetComponent<Button>();

            newButton.navigation = new Navigation { mode = Navigation.Mode.None }; // Remove the action of Enter on the button

            buttons[name] = newButton;
            uiElements[name] = UI.Button;
            rects[name] = rt;

            newButtonGO.SetActive(true);

            return newButton;
        }

        public void ChangeButtonLabel(string name, string label)
        {
            if (!buttons.ContainsKey(name))
            {
                Logger.LogError("There's no button with the name : " + name);
                return;
            }
            TMP_Text buttonLabel = buttons[name].GetComponentInChildren<TMP_Text>();
            buttonLabel.text = label;
        }

        public TMP_Dropdown GetOrCreateDropdown(string name, List<string> options, int width, int height, int posX = 0, int posY = 0, int labelFontSize = 32, int itemFontSize = 20)
        {
            if (uiElements.ContainsKey(name))
            {
                if (uiElements[name] != UI.Dropdown)
                {
                    Logger.LogError("You can't change the type of an UI element !");
                    return null;
                }
                return dropdowns[name];
            }

            GameObject referenceDropdownGO = transform.Find("Dropdown").gameObject;

            GameObject newDropdownGO = Instantiate(referenceDropdownGO, transform);
            newDropdownGO.name = name;

            RectTransform rt = newDropdownGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(posX, posY);

            TMP_Dropdown newDropdown = newDropdownGO.GetComponent<TMP_Dropdown>();

            TMP_Text newDropdownLabel = newDropdown.captionText;
            newDropdownLabel.fontSize = labelFontSize;

            Transform template = newDropdown.template;
            TMP_Text itemLabel = template.GetComponentInChildren<TMP_Text>();
            itemLabel.fontSize = itemFontSize;

            ReplaceDropdownOptions(newDropdown, options);

            newDropdown.navigation = new Navigation { mode = Navigation.Mode.None }; // Remove the action of Enter on the Dropdown

            dropdowns[name] = newDropdown;
            uiElements[name] = UI.Dropdown;
            rects[name] = rt;

            newDropdownGO.SetActive(true);

            return newDropdown;
        }

        private void ReplaceDropdownOptions(TMP_Dropdown dropdown, List<string> newOptions, int index = 0)
        {
            if (newOptions?.Count == 0)
            {
                Logger.LogError("There must be one choice at least !");
                return;
            }
            if (index < 0 || index >= newOptions.Count)
            {
                Logger.LogError("The index is out of range !");
                return;
            }
            dropdown.ClearOptions();
            dropdown.AddOptions(newOptions);
            dropdown.value = index;
            dropdown.RefreshShownValue();
        }

        public List<string> GetDropdownOptions(string name)
        {
            return dropdowns[name].options.Select(o => o.text).ToList();
        }

        public void ChangeDropdownValue(string name, int index)
        {
            if (!dropdowns.ContainsKey(name))
            {
                Logger.LogError("There's no dropdown with the name : " + name);
                return;
            }
            if (index < 0  || index >= dropdowns[name].options.Count)
            {
                Logger.LogError("The index is out of range !");
                return;
            }
            dropdowns[name].value = index;
            dropdowns[name].RefreshShownValue();
        }

        public void AddDropdownOption(string name, string option, bool acceptDuplicate = false)
        {
            if (!dropdowns.ContainsKey(name))
            {
                Logger.LogError("There's no dropdown with the name : " + name);
                return;
            }
            List<string> options = GetDropdownOptions(name);
            if (options.Contains(option) && !acceptDuplicate)
            {
                return;
            }
            options.Add(option);
            ReplaceDropdownOptions(dropdowns[name], options, dropdowns[name].value);
        }

        public void RemoveDropdownOption(string name, string optionToRemove, bool warnOptionMissing = true)
        {
            if (!dropdowns.ContainsKey(name))
            {
                Logger.LogError("There's no dropdown with the name : " + name);
                return;
            }
            List<string> options = GetDropdownOptions(name);
            if (!options.Contains(optionToRemove) && warnOptionMissing)
            {
                Logger.LogWarning("There's no option called : " + optionToRemove);
                return;
            }

            int index = dropdowns[name].value;
            int indexOptionToRemove = options.IndexOf(optionToRemove);
            index = index == indexOptionToRemove ? 0 : (index < indexOptionToRemove ? index : index - 1);

            options.Remove(optionToRemove);

            ReplaceDropdownOptions(dropdowns[name], options, index);
        }

        public void ChangeDropdownOptions(string name, List<string> options, int index = 0)
        {
            if (!dropdowns.ContainsKey(name))
            {
                Logger.LogError("There's no dropdown with the name : " + name);
                return;
            }
            ReplaceDropdownOptions(dropdowns[name], options, index);
        }

        //
        // Listeners
        //

        public void AddListener<TValue>(Action<TValue> callback, UnityEvent<TValue> @event)
        {
            @event.AddListener(value =>
            {
                callback(value);

                if (correctOutputValue)
                {
                    if (_processorUI != null || !needConfirmation)
                        WriteData();
                }

                if (applyButton != null)
                {
                    applyButton.interactable = correctOutputValue;
                }

                SetDirty(correctOutputValue);
            });
        }

        public void RemoveListener<TValue>(Action<TValue> callback, UnityEvent<TValue> @event)
        {
            @event.RemoveListener(value =>
            {
                callback(value);
            });
        }

        // Add and remove listeners for buttons
        public void AddListener(Action callback, UnityEvent @event)
        {
            @event.AddListener(() =>
            {
                callback();

                if (correctOutputValue)
                {
                    if (_processorUI != null || !needConfirmation)
                        WriteData();
                }

                if (applyButton != null)
                {
                    applyButton.interactable = correctOutputValue;
                }

                SetDirty(correctOutputValue);
            });
        }

        public void RemoveListener(Action callback, UnityEvent @event)
        {
            @event.RemoveListener(() =>
            {
                callback();
            });
        }

        //
        // Position UI elements
        //

        public enum RelativePlacement
        {
            RightOf,
            LeftOf,
            Above,
            Below
        }

        // Postion an element relative to a target
        public void PlaceRelativeTo(string elementName, string targetName, RelativePlacement placement, float distance = 0f)
        {
            if (!rects.TryGetValue(elementName, out RectTransform element))
                throw new ArgumentException($"UI element '{elementName}' not found.");

            if (!rects.TryGetValue(targetName, out RectTransform target))
                throw new ArgumentException($"UI element '{targetName}' not found.");

            if (element.parent != target.parent)
                throw new InvalidOperationException("Relative placement requires both elements to share the same parent.");

            Vector2 targetPos = target.anchoredPosition;
            Vector2 targetSize = target.sizeDelta;
            Vector2 elementSize = element.sizeDelta;

            Vector2 newPos = targetPos;

            switch (placement)
            {
                case RelativePlacement.RightOf:
                    newPos.x = targetPos.x + targetSize.x + distance;
                    newPos.y = targetPos.y;
                    break;

                case RelativePlacement.LeftOf:
                    newPos.x = targetPos.x - elementSize.x - distance;
                    newPos.y = targetPos.y;
                    break;

                case RelativePlacement.Above:
                    newPos.x = targetPos.x;
                    newPos.y = targetPos.y + targetSize.y + distance;
                    break;

                case RelativePlacement.Below:
                    newPos.x = targetPos.x;
                    newPos.y = targetPos.y - elementSize.y - distance;
                    break;
            }

            element.anchoredPosition = newPos;
        }

        public enum EditorSide
        {
            Left,
            Right,
            Top,
            Bottom
        }

        public void SetMarginFromEditorSide(string elementName, EditorSide side, float margin)
        {
            if (!rects.TryGetValue(elementName, out RectTransform element))
                throw new ArgumentException($"UI element '{elementName}' not found.");

            RectTransform rectBackground = background.GetComponent<RectTransform>();

            Vector2 pos = element.anchoredPosition;
            Vector2 elementSize = element.sizeDelta;
            Vector2 bgSize = rectBackground.sizeDelta;

            switch (side)
            {
                case EditorSide.Left:
                    pos.x = margin;
                    break;

                case EditorSide.Right:
                    pos.x = bgSize.x - elementSize.x - margin;
                    break;

                case EditorSide.Bottom:
                    pos.y = margin;
                    break;

                case EditorSide.Top:
                    pos.y = bgSize.y - elementSize.y - margin;
                    break;
            }

            element.anchoredPosition = pos;
        }

        public void PlaceRelativeToEditorCenter(string elementName, Vector2 offset)
        {
            if (!rects.TryGetValue(elementName, out RectTransform element))
                throw new ArgumentException($"UI element '{elementName}' not found.");

            RectTransform rectBackground = background.GetComponent<RectTransform>();

            Vector2 bgSize = rectBackground.sizeDelta;
            Vector2 elementSize = element.sizeDelta;

            Vector2 centerPos = new Vector2(
                (bgSize.x - elementSize.x) * 0.5f,
                (bgSize.y - elementSize.y) * 0.5f
            );

            element.anchoredPosition = centerPos + offset;
        }

        //
        //
        //

        private enum UI
        {
            InputField,
            Label,
            Hint,
            Toggle,
            Image,
            Slider,
            Button,
            Dropdown
        }

        private GameObject background;

        private Dictionary<string, UI> uiElements = new Dictionary<string, UI>();

        private Dictionary<string, RectTransform> rects = new Dictionary<string, RectTransform>();

        private Dictionary<string, TMP_InputField> inputFields = new Dictionary<string, TMP_InputField>();

        private Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();

        private Dictionary<string, TMP_Text> hints = new Dictionary<string, TMP_Text>();

        private Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();

        private Dictionary<string, Image> images = new Dictionary<string, Image>();

        private Dictionary<string, Slider> sliders = new Dictionary<string, Slider>();

        private Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        private Dictionary<string, TMP_Dropdown> dropdowns = new Dictionary<string, TMP_Dropdown>();

        public Vector2 editorSize;

        public string typeName;

        public T outputValue;

        public bool correctOutputValue = true;

        public bool needConfirmation = true;
    }
}
