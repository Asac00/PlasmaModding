using TMPro;
using UnityEngine;

namespace PlasmaModding.UI
{
    public class SplitButtonDropdownBehaviour : MonoBehaviour
    {
        public TMP_Dropdown dropdown;
        public TMP_Text buttonLabel;

        private void Awake()
        {
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            buttonLabel.text = dropdown.options[dropdown.value].text;
        }

        private void OnDestroy()
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        private void OnDropdownValueChanged(int index)
        {
            buttonLabel.text = dropdown.options[index].text;
        }
    }
}
