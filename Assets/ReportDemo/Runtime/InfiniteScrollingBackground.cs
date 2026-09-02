using UnityEngine;
using UnityEngine.UI;

namespace Android2D.ReportDemo
{
    /// <summary>
    /// Slide 20 technique: two identical backgrounds move left; a panel that leaves
    /// the screen is placed immediately after the other panel for an endless loop.
    /// </summary>
    public sealed class InfiniteScrollingBackground : MonoBehaviour
    {
        [SerializeField] private float speed = 105f;

        private RectTransform viewport;
        private RectTransform first;
        private RectTransform second;
        private float panelWidth;
        private Vector2 lastViewportSize;
        private bool running = true;

        public void Initialize(Texture texture, float pixelsPerSecond)
        {
            viewport = GetComponent<RectTransform>();
            speed = pixelsPerSecond;
            first = CreatePanel("Background A", texture);
            second = CreatePanel("Background B", texture);
            RebuildLayout(true);
        }

        public void SetRunning(bool value)
        {
            running = value;
        }

        private void Update()
        {
            if (viewport == null || first == null || second == null)
            {
                return;
            }

            Vector2 viewportSize = viewport.rect.size;
            if ((viewportSize - lastViewportSize).sqrMagnitude > 1f)
            {
                RebuildLayout(false);
            }

            if (!running)
            {
                return;
            }

            float distance = speed * Time.unscaledDeltaTime;
            first.anchoredPosition += Vector2.left * distance;
            second.anchoredPosition += Vector2.left * distance;

            WrapIfNeeded(first, second);
            WrapIfNeeded(second, first);
        }

        private RectTransform CreatePanel(string panelName, Texture texture)
        {
            RectTransform rect = RuntimeUiFactory.Rect(panelName, transform, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, Vector2.zero);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return rect;
        }

        private void RebuildLayout(bool resetPositions)
        {
            if (viewport == null)
            {
                return;
            }

            lastViewportSize = viewport.rect.size;
            panelWidth = Mathf.Max(1f, lastViewportSize.x);
            float panelHeight = Mathf.Max(1f, lastViewportSize.y);
            first.sizeDelta = new Vector2(panelWidth, panelHeight);
            second.sizeDelta = new Vector2(panelWidth, panelHeight);

            if (resetPositions || Mathf.Abs(second.anchoredPosition.x - first.anchoredPosition.x) < panelWidth * 0.5f)
            {
                first.anchoredPosition = Vector2.zero;
                second.anchoredPosition = new Vector2(panelWidth, 0f);
            }
            else
            {
                RectTransform left = first.anchoredPosition.x <= second.anchoredPosition.x ? first : second;
                RectTransform right = left == first ? second : first;
                right.anchoredPosition = new Vector2(left.anchoredPosition.x + panelWidth, 0f);
            }
        }

        private void WrapIfNeeded(RectTransform candidate, RectTransform other)
        {
            if (candidate.anchoredPosition.x + panelWidth * 0.5f <= -panelWidth * 0.5f)
            {
                candidate.anchoredPosition = new Vector2(other.anchoredPosition.x + panelWidth, 0f);
            }
        }
    }
}
