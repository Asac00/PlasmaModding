using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PlasmaModding.UI
{
    public class ToggleBehavior : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public TextMeshProUGUI text;
        public Toggle toggle;
        public GameObject toggleHighlight;

        //Color yellow = new Color(254, 172, 1);
        Color yellow = new Color(254, 172, 1);
        Color lightBlue = new Color(0, 205, 254);
        Color white = new Color(163, 241, 252);

        public void OnPointerEnter(PointerEventData eventData)
        {
            text.color = yellow;
            toggleHighlight.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            text.color = lightBlue;
            toggleHighlight.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            text.color = white;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            text.color = yellow;
            toggle.isOn = toggle.isOn ? false : true;
        }
    }
}
