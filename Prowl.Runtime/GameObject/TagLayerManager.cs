﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

using Prowl.Runtime.Cloning;
using Prowl.Runtime.Utils;

namespace Prowl.Runtime;

/// <summary>
/// Manages tags and layers for GameObjects in the Prowl Game Engine.
/// This class is a ScriptableSingleton, ensuring only one instance exists.
/// The tags and layers data is stored in a file named "TagAndLayers.setting".
/// </summary>
[FilePath("TagAndLayers.setting", FilePathAttribute.Location.Setting)]
public class TagLayerManager : ScriptableSingleton<TagLayerManager>
{
    /// <summary>
    /// List of available tags for GameObjects.
    /// </summary>
    [ListDrawer(false, true, false)]
    public List<string> tags =
    [
        "Untagged",
        "Main Camera",
        "Player",
        "Editor Only",
        "Re-spawn",
        "Finish",
        "Game Controller"
    ];

    /// <summary>
    /// Array of available layers for GameObjects.
    /// </summary>
    [ListDrawer(false, false, false)]
    public string[] layers =
    [
        "Default",
        "TransparentFX",
        "Ignore Raycast",
        "Water",
        "", "", "", "", "",
        "", "", "", "", "",
        "", "", "", "", "",
        "", "", "", "", "",
        "", "", "", "", "",
        "", "", ""
    ];

    /// <summary>
    /// Retrieves the tag name associated with the given index.
    /// </summary>
    /// <param name="index">The index of the tag to retrieve.</param>
    /// <returns>The tag name at the specified index, or "Untagged" if the index is out of range.</returns>
    public static string GetTag(int index)
    {
        if (index < 0 || index >= Instance.tags.Count)
            return "Untagged";
        return Instance.tags[index];
    }

    /// <summary>
    /// Retrieves the layer name associated with the given index.
    /// </summary>
    /// <param name="index">The index of the layer to retrieve.</param>
    /// <returns>The layer name at the specified index, "Default" If index is out of range.</returns>
    public static string GetLayer(int index)
    {
        if (index < 0 || index >= Instance.layers.Length)
            return "Default";
        return Instance.layers[index];
    }

    /// <summary>
    /// Retrieves the index of the specified tag.
    /// </summary>
    /// <param name="tag">The tag name to look up.</param>
    /// <returns>The index of the tag, or -1 if the tag is not found.</returns>
    public static int GetTagIndex(string tag)
    {
        for (int i = 0; i < Instance.tags.Count; i++)
            if (Instance.tags[i].Equals(tag, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    /// <summary>
    /// Retrieves the index of the specified layer.
    /// </summary>
    /// <param name="layer">The layer name to look up.</param>
    /// <returns>The index of the layer, or -1 if the layer is not found.</returns>
    public static int GetLayerIndex(string layer)
    {
        for (int i = 0; i < Instance.layers.Length; i++)
            if (Instance.layers[i].Equals(layer, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    /// <summary>
    /// Retrieves a copy of the layers array.
    /// </summary>
    /// <returns>A new array containing all layer names.</returns>
    public static IReadOnlyList<string> GetLayers() => Instance.layers;

    [GUIButton("Reset to Default")]
    public static void ResetDefault()
    {
        // Shortcut to reset values
        _instance = new TagLayerManager();
        _instance.Save();
    }
}
