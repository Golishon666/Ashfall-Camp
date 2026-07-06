using System;
using UnityEngine;

namespace AshfallCamp.Editor.FigmaImport
{
    public static class FigmaSliderDetector
    {
        public static bool TryCreate(FigmaImportNode node, out FigmaSliderDefinition definition)
        {
            definition = default;
            if (node == null || node.children == null || node.children.Count == 0) return false;

            var nodeName = FigmaImportNameUtility.NormalizeForSearch(node.name);
            var looksLikeSlider = nodeName.Contains("slider") ||
                                  nodeName.Contains("progress") ||
                                  nodeName.Contains("capacitybar") ||
                                  nodeName.Contains("durability");

            FigmaImportNode track = null;
            FigmaImportNode fill = null;
            FigmaImportNode handle = null;

            for (var i = 0; i < node.children.Count; i++)
            {
                var child = node.children[i];
                if (child == null) continue;

                var name = FigmaImportNameUtility.NormalizeForSearch(child.name);
                if (track == null && name.Contains("track"))
                {
                    track = child;
                }
                else if (fill == null && name.Contains("fill"))
                {
                    fill = child;
                }
                else if (handle == null && name.Contains("handle"))
                {
                    handle = child;
                }
            }

            if (!looksLikeSlider && track == null && fill == null) return false;
            if (track == null || fill == null) return false;
            if (track.bounds.width <= 0f) return false;

            definition = new FigmaSliderDefinition
            {
                node = node,
                track = track,
                fill = fill,
                handle = handle,
                value = Mathf.Clamp01(fill.bounds.width / track.bounds.width)
            };

            return true;
        }
    }

    public struct FigmaSliderDefinition
    {
        public FigmaImportNode node;
        public FigmaImportNode track;
        public FigmaImportNode fill;
        public FigmaImportNode handle;
        public float value;
    }
}
