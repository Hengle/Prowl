using System.Reflection;

namespace Prowl.Editor;

public struct EntrypointScript
{
    public Assembly referenceAssembly;
    public FileInfo startupScript;
    public string startupObjectName;


    /// <summary>
    /// Creates a shim file that invokes Main on the given type
    /// </summary>
    public static EntrypointScript Create(Type entrypointType)
    {
        MethodInfo? entrypoint = entrypointType.GetMethod("Main", BindingFlags.Static) ??
            throw new Exception($"Type {entrypointType} does not contain a Main method");

        EntrypointScript script = new();

        string startupName = $"{entrypointType.FullName.Replace('.', '_')}_Entrypoint";

        string scriptPath = Path.Combine(Project.Active.TempDirectory.FullName, $"{startupName}.cs");

        script.referenceAssembly = entrypointType.Assembly;
        script.startupScript = new FileInfo(scriptPath);
        script.startupObjectName = startupName;

        string args = "";

        if (entrypoint.GetParameters().Length > 0)
        {
            if (entrypoint.GetParameters().Length != 1 || entrypoint.GetParameters()[0].ParameterType != typeof(string[]))
                throw new Exception($"Type {entrypointType}'s Main method contains invalid parameters. Must be empty or string[].");

            args = "args";
        }

        string returnType = entrypoint.ReturnType == typeof(void) ? "void" : entrypoint.ReturnType.FullName!;
        string returns = entrypoint.ReturnType == typeof(void) ? "" : "return";

        File.WriteAllText(script.startupScript.FullName,
$$"""
public static class {{startupName}}
{
    public static {{returnType}} Main(string[] args)
    {
        {{returns}} {{entrypoint.DeclaringType.FullName}}.{{entrypoint.Name}}({{args}});
    }
}
"""
        );

        return script;
    }
}
