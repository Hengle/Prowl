﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime.NodeSystem;

[Node("GameObject/Set Enabled")]
public class SetEnabledNode : InOutFlowNode
{
    public override bool ShowTitle => true;
    public override string Title => "Set Enabled";
    public override float Width => 100;

    [Input(ShowBackingValue.Never)] public GameObject Target;
    [Input] public bool Enabled = true;

    public override void Execute(NodePort input)
    {
        GameObject t = GetInputValue("Target", Target);
        bool enabled = GetInputValue("Enabled", Enabled);

        if (t != null)
        {
            t.enabled = enabled;
        }

        ExecuteNext();
    }
}
