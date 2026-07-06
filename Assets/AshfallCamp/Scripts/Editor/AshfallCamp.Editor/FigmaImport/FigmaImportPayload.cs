using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace AshfallCamp.Editor.FigmaImport
{
    [Serializable]
    public sealed class FigmaImportDocument
    {
        public string source;
        public string expectedFileName;
        public string currentFileName;
        public string currentFileKey;
        public string port;
        public string exportedAt;
        public List<FigmaImportFrame> frames = new List<FigmaImportFrame>();

        public static FigmaImportDocument FromJson(string json)
        {
            return JsonConvert.DeserializeObject<FigmaImportDocument>(json);
        }

        public FigmaImportFrame FindFrame(string keyOrSlug)
        {
            if (frames == null || string.IsNullOrWhiteSpace(keyOrSlug)) return null;

            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                if (frame == null) continue;

                if (string.Equals(frame.key, keyOrSlug, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(frame.slug, keyOrSlug, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(frame.frameName, keyOrSlug, StringComparison.OrdinalIgnoreCase))
                {
                    return frame;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class FigmaImportFrame
    {
        public string key;
        public string slug;
        public string frameId;
        public string frameName;
        public float width;
        public float height;
        public FigmaBounds rootBounds;
        public int nodeCount;
        public int textCount;
        public int visualCount;
        public FigmaImportNode tree;
        public List<FigmaNodeExport> exports = new List<FigmaNodeExport>();
        public string screenshotPath;
    }

    [Serializable]
    public sealed class FigmaImportNode
    {
        public string id;
        public string parentId;
        public string name;
        public string type;
        public int depth;
        public int siblingIndex;
        public string path;
        public string renderMode;
        public bool clipsContent;
        public float opacity = 1f;
        public string blendMode;
        public FigmaBounds bounds;
        public List<FigmaPaint> fills = new List<FigmaPaint>();
        public List<FigmaPaint> strokes = new List<FigmaPaint>();
        public float strokeWeight;
        public float cornerRadius;
        public FigmaTextData text;
        public List<FigmaImportNode> children = new List<FigmaImportNode>();
        public string assetPath;

        public bool IsText => string.Equals(type, "TEXT", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(renderMode, "text", StringComparison.OrdinalIgnoreCase);

        public bool HasChildren => children != null && children.Count > 0;
    }

    [Serializable]
    public sealed class FigmaNodeExport
    {
        public string id;
        public string name;
        public string mode;
        public int index;
        public int scale;
        public int byteLength;
        public string error;
        public string assetPath;
    }

    [Serializable]
    public struct FigmaBounds
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public FigmaBounds(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float xMax => x + width;
        public float yMax => y + height;
        public Vector2 size => new Vector2(width, height);
        public Vector2 center => new Vector2(x + width * 0.5f, y + height * 0.5f);

        public bool ContainsCenterOf(FigmaBounds other)
        {
            var c = other.center;
            return c.x >= x && c.x <= xMax && c.y >= y && c.y <= yMax;
        }
    }

    [Serializable]
    public sealed class FigmaPaint
    {
        public string type;
        public float opacity = 1f;
        public FigmaColor color;
        public bool hasImage;
        public string scaleMode;
    }

    [Serializable]
    public struct FigmaColor
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }

    [Serializable]
    public sealed class FigmaTextData
    {
        public string characters;
        public FigmaFontName fontName;
        public float fontSize = 16f;
        public FigmaLineHeight lineHeight;
        public FigmaLetterSpacing letterSpacing;
        public string textAlignHorizontal;
        public string textAlignVertical;
        public string textAutoResize;
    }

    [Serializable]
    public sealed class FigmaFontName
    {
        public string family;
        public string style;
    }

    [Serializable]
    public sealed class FigmaLineHeight
    {
        public string unit;
        public float value;
    }

    [Serializable]
    public sealed class FigmaLetterSpacing
    {
        public string unit;
        public float value;
    }
}
