using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AshfallCamp.Editor.FigmaImport
{
    public static class FigmaImportNameUtility
    {
        private static readonly Regex NoisePattern = new Regex(@"\[(background|visual|container|text)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SeparatorPattern = new Regex(@"[^A-Za-z0-9]+", RegexOptions.Compiled);

        public static string SanitizeUnityName(string figmaName)
        {
            var value = string.IsNullOrWhiteSpace(figmaName) ? "Node" : figmaName.Trim();
            value = NoisePattern.Replace(value, string.Empty);
            value = value.Replace(" / ", " ");
            value = value.Replace("/", " ");
            value = value.Replace("\\", " ");

            string prefix = null;
            var dashIndex = value.IndexOf(" - ", StringComparison.Ordinal);
            if (dashIndex > 0)
            {
                prefix = ToPascal(value.Substring(0, dashIndex));
                value = value.Substring(dashIndex + 3);
            }

            var cleaned = ToPascal(value);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                cleaned = "Node";
            }

            return string.IsNullOrWhiteSpace(prefix) ? cleaned : prefix + "_" + cleaned;
        }

        public static string NormalizeForSearch(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var sanitized = SeparatorPattern.Replace(value, string.Empty);
            return sanitized.ToLowerInvariant();
        }

        public static string NormalizeForSearch(Transform transform)
        {
            return transform == null ? string.Empty : NormalizeForSearch(transform.name);
        }

        public static bool Matches(Transform transform, params string[] tokens)
        {
            if (transform == null || tokens == null || tokens.Length == 0) return false;
            var normalized = NormalizeForSearch(transform);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = NormalizeForSearch(tokens[i]);
                if (token.Length > 0 && normalized.Contains(token))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ToPascal(string value)
        {
            value = SeparatorPattern.Replace(value ?? string.Empty, " ").Trim();
            if (value.Length == 0) return string.Empty;

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder();
            var textInfo = CultureInfo.InvariantCulture.TextInfo;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length == 0) continue;

                var upper = part.ToUpperInvariant();
                if (upper == "HP" || upper == "XP" || upper == "UI")
                {
                    builder.Append(upper[0]);
                    for (var c = 1; c < upper.Length; c++)
                    {
                        builder.Append(char.ToLowerInvariant(upper[c]));
                    }
                    continue;
                }

                builder.Append(textInfo.ToTitleCase(part.ToLowerInvariant()));
            }

            return builder.ToString();
        }
    }
}
