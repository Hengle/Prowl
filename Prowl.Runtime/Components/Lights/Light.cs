﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Runtime.InteropServices;

using Prowl.Runtime.Rendering.Pipelines;

namespace Prowl.Runtime;

public abstract class Light : MonoBehaviour, IRenderableLight
{

    public Color color = Color.white;
    public float intensity = 8.0f;
    public float shadowBias = 0.00001f;
    public float shadowNormalBias = 0.0025f;
    public bool castShadows = true;


    public override void Update()
    {
        RenderPipeline.AddLight(this);
    }

    public virtual int GetLightID() => this.InstanceID;
    public abstract LightType GetLightType();
    public virtual Vector3 GetLightPosition() => Transform.position;
    public virtual Vector3 GetLightDirection() => Transform.forward;
    public virtual bool DoCastShadows() => castShadows;
    public abstract void GetShadowMatrix(out Matrix4x4 view, out Matrix4x4 projection);

    public abstract GPULight GetGPULight(int res, bool cameraRelative, Vector3 cameraPosition);
}
