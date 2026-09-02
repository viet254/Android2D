using System;
using UnityEngine;
using UnityEngine.UI;

namespace Android2D.ReportDemo
{
    /// <summary>
    /// Creates the demo's vector-like UI and small procedural sprites at runtime.
    /// </summary>
    public static class RuntimeUiFactory
    {
        private static Font cachedFont;
        private static Sprite cachedWhiteSprite;

        public static Font Font
        {
            get
            {
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return cachedFont;
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (cachedWhiteSprite == null)
                {
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.name = "Report Demo White Pixel";
                    texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    texture.Apply();
                    cachedWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f, 2f);
                }
                return cachedWhiteSprite;
            }
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image Image(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Text(string name, Transform parent, string value, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static ReportButton Button(string name, Transform parent, string label, Color normal, Color pressed,
            Action onClick, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            Image image = Image(name, parent, normal, anchorMin, anchorMax, offsetMin, offsetMax);
            image.raycastTarget = true;
            Text(name + " Label", image.transform, label, 28, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            ReportButton button = image.gameObject.AddComponent<ReportButton>();
            button.Configure(normal, pressed, onClick);
            return button;
        }

        public static Sprite CreateLogoSprite()
        {
            const int size = 256;
            Color32[] pixels = TransparentPixels(size, size);
            Color32 gold = new Color32(255, 194, 72, 255);
            Color32 cyan = new Color32(78, 226, 255, 255);
            Color32 navy = new Color32(13, 24, 49, 255);

            for (int y = 20; y < 236; y++)
            {
                for (int x = 20; x < 236; x++)
                {
                    float nx = (x - 128f) / 108f;
                    float ny = (y - 128f) / 108f;
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    if (radius <= 1f)
                    {
                        pixels[y * size + x] = radius > 0.86f ? gold : navy;
                    }
                }
            }

            for (int y = 54; y < 204; y++)
            {
                float halfWidth = Mathf.Lerp(70f, 20f, Mathf.InverseLerp(54f, 204f, y));
                for (int x = 128 - Mathf.RoundToInt(halfWidth); x <= 128 + Mathf.RoundToInt(halfWidth); x++)
                {
                    pixels[y * size + x] = cyan;
                }
            }

            for (int i = -9; i <= 9; i++)
            {
                for (int j = -42; j <= 42; j++)
                {
                    int x = 128 + i + j;
                    int y = 128 + i - j;
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = gold;
                    }
                }
            }

            return SpriteFromPixels("Echo Runner Logo", size, size, pixels, 256f);
        }

        public static Texture2D CreateBackgroundTexture()
        {
            const int width = 640;
            const int height = 360;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Seamless Twilight Background";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float vertical = y / (height - 1f);
                Color sky = Color.Lerp(new Color(0.025f, 0.045f, 0.13f), new Color(0.26f, 0.16f, 0.43f), vertical);
                for (int x = 0; x < width; x++)
                {
                    float phase = x / (float)width * Mathf.PI * 2f;
                    float farMountain = 105f + Mathf.Sin(phase * 2f) * 28f + Mathf.Sin(phase * 5f) * 10f;
                    float nearMountain = 65f + Mathf.Sin(phase * 3f + 0.8f) * 34f + Mathf.Sin(phase * 7f) * 8f;
                    Color color = sky;
                    if (y < farMountain)
                    {
                        color = new Color(0.085f, 0.12f, 0.24f);
                    }
                    if (y < nearMountain)
                    {
                        color = new Color(0.025f, 0.07f, 0.12f);
                    }

                    float star = Mathf.Repeat(x * 0.75487766f + y * 0.5698403f, 37f);
                    if (y > 165 && star < 0.16f)
                    {
                        color = new Color(0.85f, 0.95f, 1f);
                    }
                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        public static Sprite CreatePlayerSprite()
        {
            return CreateCharacterSprite("Runner", new Color32(71, 222, 255, 255), new Color32(255, 195, 75, 255));
        }

        public static Sprite CreateEnemySprite()
        {
            return CreateCharacterSprite("Shadow", new Color32(242, 88, 119, 255), new Color32(117, 45, 88, 255));
        }

        public static Sprite CreateCrystalSprite()
        {
            const int width = 64;
            const int height = 80;
            Color32[] pixels = TransparentPixels(width, height);
            Color32 outer = new Color32(255, 200, 69, 255);
            Color32 inner = new Color32(255, 245, 170, 255);
            for (int y = 5; y < 75; y++)
            {
                float t = Mathf.Abs(y - 40f) / 35f;
                int half = Mathf.RoundToInt(Mathf.Lerp(25f, 2f, t));
                for (int x = 32 - half; x <= 32 + half; x++)
                {
                    pixels[y * width + x] = Mathf.Abs(x - 32) < half * 0.35f ? inner : outer;
                }
            }
            return SpriteFromPixels("Energy Crystal", width, height, pixels, 100f);
        }

        private static Sprite CreateCharacterSprite(string name, Color32 body, Color32 accent)
        {
            const int width = 96;
            const int height = 128;
            Color32[] pixels = TransparentPixels(width, height);
            Color32 dark = new Color32(12, 20, 35, 255);

            FillEllipse(pixels, width, height, 48, 94, 23, 25, body);
            FillEllipse(pixels, width, height, 48, 105, 13, 13, accent);
            FillRect(pixels, width, height, 29, 42, 66, 92, body);
            FillRect(pixels, width, height, 25, 18, 40, 47, dark);
            FillRect(pixels, width, height, 56, 18, 71, 47, dark);
            FillRect(pixels, width, height, 18, 54, 31, 82, accent);
            FillRect(pixels, width, height, 65, 54, 78, 82, accent);
            FillRect(pixels, width, height, 39, 99, 44, 104, dark);
            FillRect(pixels, width, height, 54, 99, 59, 104, dark);
            return SpriteFromPixels(name, width, height, pixels, 100f);
        }

        private static Color32[] TransparentPixels(int width, int height)
        {
            Color32[] pixels = new Color32[width * height];
            Color32 transparent = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = transparent;
            }
            return pixels;
        }

        private static void FillRect(Color32[] pixels, int width, int height, int xMin, int yMin, int xMax, int yMax, Color32 color)
        {
            for (int y = Mathf.Max(0, yMin); y < Mathf.Min(height, yMax); y++)
            {
                for (int x = Mathf.Max(0, xMin); x < Mathf.Min(width, xMax); x++)
                {
                    pixels[y * width + x] = color;
                }
            }
        }

        private static void FillEllipse(Color32[] pixels, int width, int height, int centerX, int centerY,
            int radiusX, int radiusY, Color32 color)
        {
            for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    float nx = (x - centerX) / (float)radiusX;
                    float ny = (y - centerY) / (float)radiusY;
                    if (x >= 0 && x < width && y >= 0 && y < height && nx * nx + ny * ny <= 1f)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        private static Sprite SpriteFromPixels(string name, int width, int height, Color32[] pixels, float pixelsPerUnit)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name + " Texture";
            texture.filterMode = FilterMode.Point;
            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f, pixelsPerUnit);
            sprite.name = name;
            return sprite;
        }
    }
}
