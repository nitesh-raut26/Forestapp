using UnityEngine;
using TMPro;

namespace ForestFriendsQuest
{
    [AddComponentMenu("UI/Text (Wrapper)", 11)]
    public class Text : TextMeshProUGUI
    {
        public new UnityEngine.TextAnchor alignment
        {
            get => MapAlignmentInverse(base.alignment);
            set => base.alignment = MapAlignment(value);
        }

        public new UnityEngine.FontStyle fontStyle
        {
            get => MapFontStyleInverse(base.fontStyle);
            set => base.fontStyle = MapFontStyle(value);
        }

        public bool resizeTextForBestFit
        {
            get => base.enableAutoSizing;
            set => base.enableAutoSizing = value;
        }

        public static TMPro.TextAlignmentOptions MapAlignment(UnityEngine.TextAnchor anchor)
        {
            switch (anchor)
            {
                case UnityEngine.TextAnchor.UpperLeft: return TMPro.TextAlignmentOptions.TopLeft;
                case UnityEngine.TextAnchor.UpperCenter: return TMPro.TextAlignmentOptions.Top;
                case UnityEngine.TextAnchor.UpperRight: return TMPro.TextAlignmentOptions.TopRight;
                case UnityEngine.TextAnchor.MiddleLeft: return TMPro.TextAlignmentOptions.Left;
                case UnityEngine.TextAnchor.MiddleCenter: return TMPro.TextAlignmentOptions.Center;
                case UnityEngine.TextAnchor.MiddleRight: return TMPro.TextAlignmentOptions.Right;
                case UnityEngine.TextAnchor.LowerLeft: return TMPro.TextAlignmentOptions.BottomLeft;
                case UnityEngine.TextAnchor.LowerCenter: return TMPro.TextAlignmentOptions.Bottom;
                case UnityEngine.TextAnchor.LowerRight: return TMPro.TextAlignmentOptions.BottomRight;
                default: return TMPro.TextAlignmentOptions.Center;
            }
        }

        public static UnityEngine.TextAnchor MapAlignmentInverse(TMPro.TextAlignmentOptions option)
        {
            switch (option)
            {
                case TMPro.TextAlignmentOptions.TopLeft: return UnityEngine.TextAnchor.UpperLeft;
                case TMPro.TextAlignmentOptions.Top: return UnityEngine.TextAnchor.UpperCenter;
                case TMPro.TextAlignmentOptions.TopRight: return UnityEngine.TextAnchor.UpperRight;
                case TMPro.TextAlignmentOptions.Left: return UnityEngine.TextAnchor.MiddleLeft;
                case TMPro.TextAlignmentOptions.Center: return UnityEngine.TextAnchor.MiddleCenter;
                case TMPro.TextAlignmentOptions.Right: return UnityEngine.TextAnchor.MiddleRight;
                case TMPro.TextAlignmentOptions.BottomLeft: return UnityEngine.TextAnchor.LowerLeft;
                case TMPro.TextAlignmentOptions.Bottom: return UnityEngine.TextAnchor.LowerCenter;
                case TMPro.TextAlignmentOptions.BottomRight: return UnityEngine.TextAnchor.LowerRight;
                default: return UnityEngine.TextAnchor.MiddleCenter;
            }
        }

        public static TMPro.FontStyles MapFontStyle(UnityEngine.FontStyle style)
        {
            switch (style)
            {
                case UnityEngine.FontStyle.Normal: return TMPro.FontStyles.Normal;
                case UnityEngine.FontStyle.Bold: return TMPro.FontStyles.Bold;
                case UnityEngine.FontStyle.Italic: return TMPro.FontStyles.Italic;
                case UnityEngine.FontStyle.BoldAndItalic: return TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
                default: return TMPro.FontStyles.Normal;
            }
        }

        public static UnityEngine.FontStyle MapFontStyleInverse(TMPro.FontStyles styles)
        {
            if ((styles & TMPro.FontStyles.Bold) != 0 && (styles & TMPro.FontStyles.Italic) != 0)
                return UnityEngine.FontStyle.BoldAndItalic;
            if ((styles & TMPro.FontStyles.Bold) != 0)
                return UnityEngine.FontStyle.Bold;
            if ((styles & TMPro.FontStyles.Italic) != 0)
                return UnityEngine.FontStyle.Italic;
            return UnityEngine.FontStyle.Normal;
        }
    }
}
