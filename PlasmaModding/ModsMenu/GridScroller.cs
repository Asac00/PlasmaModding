using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PlasmaModding.ModsMenuScripts
{
    public class GridScroller : MonoBehaviour
    {
        public RectTransform content;
        public RectTransform viewport;
        public GridLayoutGroup grid;
        public Scrollbar scrollbar;

        float scrollSpeed = 0.1f;

        private float contentHeight;
        private bool isScrollBarActive = true;

        IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();

            contentHeight = viewport.rect.height;

            float gridHeight = GetTotalContentHeight();
            if (gridHeight < contentHeight)
            {
                scrollbar.gameObject.SetActive(false);
                isScrollBarActive = false;
            }
            else
            {
                scrollbar.onValueChanged.AddListener(Scroll);
            }
        }

        void Update()
        {
            if (isScrollBarActive)
            {
                float scrollDelta = Input.mouseScrollDelta.y;

                if (Mathf.Abs(scrollDelta) > 0.01f)
                {
                    float newValue = scrollbar.value - scrollDelta * scrollSpeed;

                    if (newValue < 0) { newValue = 0; }
                    if (newValue > 1) { newValue = 1; }

                    scrollbar.value = newValue;
                    Scroll(newValue);
                }
            }
        }

        void Scroll(float value)
        {
            float gridHeight = GetTotalContentHeight();
            Vector2 offsetMax = content.offsetMax;
            offsetMax.y = (gridHeight - contentHeight) * value;
            content.offsetMax = offsetMax;
        }

        public float GetTotalContentHeight()
        {
            int childCount = grid.transform.childCount;
            int columns = Mathf.Max(1, grid.constraintCount); // fixed column count
            int rows = Mathf.CeilToInt((float)childCount / columns);

            float totalHeight =
                grid.padding.top +
                rows * grid.cellSize.y +
                (rows - 1) * grid.spacing.y +
                grid.padding.bottom;

            return totalHeight;
        }
    }
}
