﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime.NodeSystem;

[Node("GameObject/Set Scale")]
public class SetScaleNode : InOutFlowNode
{
    public override bool ShowTitle => true;
    public override string Title => "Set Scale";
    public override float Width => 100;

    [Input] public GameObject Target;
    [Input] public Vector3 Scale;

    public override void Execute(NodePort input)
    {
        GameObject t = GetInputValue("Target", Target);
        Vector3 s = GetInputValue("Scale", Scale);

        if (t != null)
        {
            t.Transform.localScale = s;
        }

        ExecuteNext();
    }
}
