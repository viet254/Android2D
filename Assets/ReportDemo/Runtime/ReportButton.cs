using System;
using UnityEngine;
using UnityEngine.UI;

namespace Android2D.ReportDemo
{
    /// <summary>
    /// Runtime button with no EventSystem dependency, useful for a generated mobile demo.
    /// </summary>
    public sealed class ReportButton : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Image background;
        private Color normalColor;
        private Color pressedColor;
        private Action clicked;
        private bool wasHeld;

        public RectTransform RectTransform => rectTransform;

        public void Configure(Color normal, Color pressed, Action onClick)
        {
            rectTransform = GetComponent<RectTransform>();
            background = GetComponent<Image>();
            normalColor = normal;
            pressedColor = pressed;
            clicked = onClick;
            background.color = normalColor;
        }

        private void Update()
        {
            bool held = ReportInput.IsHeld(rectTransform);
            if (background != null)
            {
                background.color = held ? pressedColor : normalColor;
            }

            transform.localScale = held ? Vector3.one * 0.97f : Vector3.one;

            if (held && !wasHeld && ReportInput.WasPressed(rectTransform))
            {
                clicked?.Invoke();
            }

            wasHeld = held;
        }
    }
}
