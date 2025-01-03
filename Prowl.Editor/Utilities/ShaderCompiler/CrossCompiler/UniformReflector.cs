// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime;

using SPIRVCross.NET;

using Veldrid;

using Prowl.Runtime.Rendering;

namespace Prowl.Editor;


public static partial class ShaderCrossCompiler
{
    public static Uniform[] GetUniforms(Reflector reflector, Resources resources)
    {
        List<Uniform> uniforms = new();

        foreach (var res in resources.StorageImages)
            uniforms.Add(new Uniform(CleanseName(res.name), GetBinding(reflector, res.id), ResourceKind.TextureReadWrite));

        foreach (var res in resources.SeparateImages)
            uniforms.Add(new Uniform(CleanseName(res.name), GetBinding(reflector, res.id), ResourceKind.TextureReadOnly));

        foreach (var res in resources.SeparateSamplers)
            uniforms.Add(new Uniform(CleanseName(res.name), GetBinding(reflector, res.id), ResourceKind.Sampler));

        foreach (var res in resources.StorageBuffers)
            uniforms.Add(CreateStorageBuffer(reflector, res));

        // Combined image samplers don't output any names, meaning we don't need to add uniforms for them
        // since the few platforms that care about it (old OpenGL) bind by name, meaning it's useless.
        // When cross-compiling, we will set the names of the combined samplers to the image anyways.
        // foreach (var combinedImage in resources.SampledImages);

        foreach (var res in resources.UniformBuffers)
            uniforms.Add(CreateConstantBuffer(reflector, res));

        uniforms.Sort((x, y) => x.binding.CompareTo(y.binding));

        return uniforms.ToArray();
    }


    static Uniform CreateStorageBuffer(Reflector reflector, ReflectedResource bufferResource)
    {
        uint binding = GetBinding(reflector, bufferResource.id);

        if (reflector.HasDecoration(bufferResource.id, Decoration.NonWritable))
            return new Uniform(CleanseName(bufferResource.name), binding, ResourceKind.StructuredBufferReadOnly);

        return new Uniform(CleanseName(bufferResource.name), binding, ResourceKind.StructuredBufferReadWrite);
    }


    static Uniform CreateConstantBuffer(Reflector reflector, ReflectedResource bufferResource)
    {
        uint binding = GetBinding(reflector, bufferResource.id);

        List<UniformMember> members = new();

        var decoratedType = reflector.GetTypeHandle(bufferResource.type_id);
        var baseType = reflector.GetTypeHandle(decoratedType.BaseTypeID);

        if (baseType.BaseType != BaseType.Struct)
            throw new Exception("Uniform is not a structure.");

        uint size = (uint)reflector.GetDeclaredStructSize(baseType);

        for (uint i = 0; i < baseType.MemberCount; i++)
        {
            TypeID memberID = baseType.GetMemberType(i);
            SPIRVCross.NET.Type type = reflector.GetTypeHandle(memberID);

            if (!IsPrimitiveType(type.BaseType))
                continue;

            UniformMember member;

            member.name = reflector.GetMemberName(baseType.BaseTypeID, i);
            member.bufferOffsetInBytes = reflector.StructMemberOffset(baseType, i);
            member.size = (uint)reflector.GetDeclaredStructMemberSize(baseType, i);

            member.arrayStride = 0;
            if (type.ArrayDimensions != 0)
                member.arrayStride = reflector.StructMemberArrayStride(baseType, i);

            member.matrixStride = 0;
            if (type.Columns > 1)
                member.matrixStride = reflector.StructMemberMatrixStride(baseType, i);

            member.width = type.VectorSize;
            member.height = type.Columns;

            member.type = type.BaseType switch
            {
                BaseType.Boolean or
                    BaseType.Int8 or
                    BaseType.Int16 or
                    BaseType.Int32 or
                    BaseType.Int64
                    => Runtime.Rendering.ValueType.Int,

                BaseType.Float16 or
                    BaseType.Float32 or
                    BaseType.Float64
                    => Runtime.Rendering.ValueType.Float,

                BaseType.UInt8 or
                    BaseType.UInt16 or
                    BaseType.UInt32 or
                    BaseType.UInt64
                    => Runtime.Rendering.ValueType.UInt,

                _ => throw new Exception($"Unknown BaseType while getting uniform: {type.BaseType}")
            };

            members.Add(member);
        }

        return new Uniform(CleanseName(bufferResource.name), binding, size, members.ToArray());
    }


    static string CleanseName(string name)
    {
        return name.Replace('$', '_');
    }


    static uint GetBinding(Reflector reflector, ID id)
    {
        if (!reflector.HasDecoration(id, Decoration.Binding))
            throw new Exception("Uniform does not have binding decoration");

        return reflector.GetDecoration(id, Decoration.Binding);
    }


    static bool IsPrimitiveType(BaseType type)
    {
        return
            type == BaseType.Boolean ||
            type == BaseType.Float16 ||
            type == BaseType.Float32 ||
            type == BaseType.Float64 ||
            type == BaseType.Int16 ||
            type == BaseType.Int32 ||
            type == BaseType.Int64 ||
            type == BaseType.Int8 ||
            type == BaseType.UInt16 ||
            type == BaseType.UInt32 ||
            type == BaseType.UInt64 ||
            type == BaseType.UInt8;
    }
}
