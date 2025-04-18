﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Prowl.Runtime.Utils;

using Veldrid;


namespace Prowl.Runtime.Rendering.Pipelines;

public struct RenderingData
{
    public bool IsSceneViewCamera;
    public bool DisplayGrid;
    public bool DisplayGizmo;
    public Matrix4x4 GridMatrix;
    public Color GridColor;
    public Vector3 GridSizes;
}


public struct RenderBatch
{
    public Material material;
    public List<int> renderIndices;
}


public abstract class RenderPipeline : EngineObject
{
    private static readonly List<IRenderable> s_renderables = [];
    public static int RenderableCount => s_renderables.Count;

    private static readonly Dictionary<Material, RenderBatch> s_batchedRenderables = [];

    private static readonly List<IRenderableLight> s_lights = [];


    public static void AddRenderable(IRenderable renderable)
    {
        s_renderables.Add(renderable);

        Material material = renderable.GetMaterial();

        if (!s_batchedRenderables.TryGetValue(material, out RenderBatch batch))
        {
            batch.material = material;
            batch.renderIndices = [];
            s_batchedRenderables.Add(material, batch);
        }

        batch.renderIndices.Add(s_renderables.Count - 1);
    }


    public static void AddLight(IRenderableLight light)
    {
        s_lights.Add(light);
    }


    [OnAssemblyUnload]
    public static void ClearRenderables()
    {
        s_renderables.Clear(); // Clear renderables

        foreach (RenderBatch batch in s_batchedRenderables.Values)
            batch.renderIndices.Clear(); // Clear batch indices

        s_batchedRenderables.Clear(); // Clear batch lookup
        s_lights.Clear(); // Clear lights
    }


    public static IEnumerable<RenderBatch> EnumerateBatches()
    {
        return s_batchedRenderables.Values;
    }


    public static IRenderable GetRenderable(int index)
    {
        return s_renderables[index];
    }


    public static IEnumerable<IRenderable> GetRenderables()
    {
        return s_renderables;
    }


    public static List<IRenderableLight> GetLights()
    {
        return s_lights;
    }


    public abstract void Render(Camera camera, in RenderingData data);
}
