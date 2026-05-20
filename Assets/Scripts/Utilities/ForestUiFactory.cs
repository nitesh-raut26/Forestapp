using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public static class ForestUiFactory
    {
        private static Sprite _whiteSprite;
        private static Sprite _circleSprite;

        public static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static RectTransform CreateUiObject(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;

            if (parent != null)
            {
                rect.SetParent(parent, false);
            }

            return rect;
        }

        public static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        public static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static Image CreateImage(Transform parent, string name, Color color, bool circular = false)
        {
            var rect = CreateUiObject(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = circular ? GetCircleSprite() : GetWhiteSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int fontSize,
            Color color,
            TextAnchor anchor,
            FontStyle fontStyle = FontStyle.Normal
        )
        {
            var rect = CreateUiObject(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Color backgroundColor,
            Color textColor,
            UnityAction onClick,
            int fontSize = 24
        )
        {
            var image = CreateImage(parent, name, backgroundColor);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = backgroundColor * 1.05f;
            colors.pressedColor = backgroundColor * 0.92f;
            colors.selectedColor = backgroundColor;
            colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.45f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var labelText = CreateText(image.transform, "Label", label, font, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(labelText.rectTransform, 8f, 8f, 8f, 8f);

            return button;
        }

        public static ScrollRect CreateScrollView(Transform parent, out RectTransform content)
        {
            var root = CreateUiObject("ScrollView", parent);
            Stretch(root);

            var viewportImage = CreateImage(root, "Viewport", new Color(0f, 0f, 0f, 0f));
            Stretch(viewportImage.rectTransform);
            var mask = viewportImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            content = CreateUiObject("Content", viewportImage.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(18, 18, 18, 32);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportImage.rectTransform;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            return scrollRect;
        }

        public static VerticalLayoutGroup AddVerticalLayout(
            GameObject target,
            float spacing,
            RectOffset padding = null,
            bool forceExpandWidth = true
        )
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(
            GameObject target,
            float spacing,
            RectOffset padding = null
        )
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            return layout;
        }

        public static GridLayoutGroup AddGridLayout(
            GameObject target,
            Vector2 cellSize,
            Vector2 spacing,
            int columns,
            RectOffset padding = null
        )
        {
            var layout = target.AddComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Mathf.Max(1, columns);
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperCenter;
            return layout;
        }

        public static LayoutElement AddLayout(
            GameObject target,
            float preferredHeight = -1f,
            float preferredWidth = -1f,
            float minHeight = -1f,
            float flexibleWidth = -1f
        )
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }
            if (preferredHeight >= 0f)
            {
                layout.preferredHeight = preferredHeight;
            }
            if (preferredWidth >= 0f)
            {
                layout.preferredWidth = preferredWidth;
            }
            if (minHeight >= 0f)
            {
                layout.minHeight = minHeight;
            }
            if (flexibleWidth >= 0f)
            {
                layout.flexibleWidth = flexibleWidth;
            }
            return layout;
        }

        public static Color FromHex(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
            {
                return _whiteSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
            {
                return _circleSprite;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var radius = size / 2f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _circleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size
            );
            return _circleSprite;
        }
    }
}
