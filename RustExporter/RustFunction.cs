using System.Reflection;
using System.Text;
using FFXIVClientStructs.Interop.Attributes;

namespace RustExporter;

public class RustFunction : IRustExportable
{
    public string Name { get; }
    public string OriginalName { get; }
    public string GeneratedName => $"{Owner.BaseName}_Fn_{OriginalName}";
    public RustStruct Owner { get; }
    public string? Signature { get; }

    public RustFunction(RustStruct owner, MethodInfo method)
    {
        Owner = owner;
        Name = RustTypeRef.SafeSnakeCase(method.Name);
        OriginalName = method.Name;
        Signature = method.GetCustomAttribute<MemberFunctionAttribute>()?.Signature;
    }

    public void Export(StringBuilder builder, int indentLevel)
    {
        builder.AppendLine($"{Exporter.Indent(indentLevel)}pub struct {GeneratedName};");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}impl {GeneratedName} {{");
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel + 1)}/// Returns a string containing the signature of this function.");
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel + 1)}/// If this function does not have a signature, returns `None`.");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}pub fn signature() -> Option<&'static str> {{");
        if (Signature != null)
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}Some(\"{Signature}\")");
        }
        else
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}None");
        }

        builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
    }
}