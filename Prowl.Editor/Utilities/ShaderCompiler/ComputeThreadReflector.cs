// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime;

using SPIRVCross.NET;

using Veldrid;

using Prowl.Runtime.Rendering;
using Prowl.Runtime.Rendering.Pipelines;

using SpecializationConstant = SPIRVCross.NET.SpecializationConstant;

#pragma warning disable

namespace Prowl.Editor;

public static class ComputeThreadReflector
{
    public static void GetThreadgroupSizes(Reflector reflector, out uint xSize, out uint ySize, out uint zSize)
    {
        ConstantID id = reflector.GetWorkGroupSizeSpecificationConstants(
            out SpecializationConstant x,
            out SpecializationConstant y,
            out SpecializationConstant z);

        Constant cID = reflector.GetConstantHandle(id);

        xSize = cID.GetScalarUInt32(0, 0);
        ySize = cID.GetScalarUInt32(0, 1);
        zSize = cID.GetScalarUInt32(0, 2);

        Debug.Log($"Kernel threadgroups: {xSize}, {ySize}, {zSize}");
    }
}
