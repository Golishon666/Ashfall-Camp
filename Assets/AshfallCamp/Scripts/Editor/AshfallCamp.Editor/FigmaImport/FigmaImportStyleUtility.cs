using System;
using TMPro;
using UnityEngine;

namespace AshfallCamp.Editor.FigmaImport
{
    public static class FigmaImportStyleUtility
    {
        public static bool TryGetSolidColor(FigmaImportNode node, out Color color)
        {
            color = Color.white;
            if (node == null || node.fills == null) return false;

            for (var i = 0; i < node.fills.Count; i++)
            {
                var paint = node.fills[i];
                if (paint == null || !string.Equals(paint.type, "SOLID", StringComparison.OrdinalIgnoreCase)) continue;
                color = ToColor(paint, NodeOpacity(node));
                return true;
            }

            return false;
        }

        public static bool IsSimpleSolid(FigmaImportNode node)
        {
            if (node == null || node.fills == null || node.fills.Count != 1) return false;

            var paint = node.fills[0];
            if (paint == null || paint.hasImage) return false;
            if (!string.Equals(paint.type, "SOLID", StringComparison.OrdinalIgnoreCase)) return false;

            return !string.Equals(node.type, "VECTOR", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(node.type, "BOOLEAN_OPERATION", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(node.type, "STAR", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(node.type, "POLYGON", StringComparison.OrdinalIgnoreCase);
        }

        public static Color TextColor(FigmaImportNode node)
        {
            Color color;
            return TryGetSolidColor(node, out color) ? color : Color.black;
        }

        public static TextAlignmentOptions TextAlignment(FigmaTextData text)
        {
            var horizontal = (text != null ? text.textAlignHorizontal : null) ?? "LEFT";
            var vertical = (text != null ? text.textAlignVertical : null) ?? "TOP";

            var isCenter = string.Equals(horizontal, "CENTER", StringComparison.OrdinalIgnoreCase);
            var isRight = string.Equals(horizontal, "RIGHT", StringComparison.OrdinalIgnoreCase);
            var isMiddle = string.Equals(vertical, "CENTER", StringComparison.OrdinalIgnoreCase);
            var isBottom = string.Equals(vertical, "BOTTOM", StringComparison.OrdinalIgnoreCase);

            if (isMiddle && isCenter) return TextAlignmentOptions.Center;
            if (isMiddle && isRight) return TextAlignmentOptions.Right;
            if (isMiddle) return TextAlignmentOptions.Left;
            if (isBottom && isCenter) return TextAlignmentOptions.Bottom;
            if (isBottom && isRight) return TextAlignmentOptions.BottomRight;
            if (isBottom) return TextAlignmentOptions.BottomLeft;
            if (isCenter) return TextAlignmentOptions.Top;
            if (isRight) return TextAlignmentOptions.TopRight;
            return TextAlignmentOptions.TopLeft;
        }

        public static float NodeOpacity(FigmaImportNode node)
        {
            return node == null || node.opacity <= 0f ? 1f : Mathf.Clamp01(node.opacity);
        }

        public static Color ToColor(FigmaPaint paint, float nodeOpacity)
        {
            if (paint == null) return Color.white;

            var paintOpacity = paint.opacity <= 0f ? 1f : Mathf.Clamp01(paint.opacity);
            var color = new Color(
                Mathf.Clamp01(paint.color.r),
                Mathf.Clamp01(paint.color.g),
                Mathf.Clamp01(paint.color.b),
                Mathf.Clamp01(paintOpacity * nodeOpacity));
            return color;
        }
    }
}
