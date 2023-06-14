using System.Reflection;
using System.Text;
using FFXIVClientStructs.Interop.Attributes;

namespace RustExporter;

public class RustFunction : IRustExportable
{
    public string Name { get; }
    public string OriginalName { get; }
    public string GeneratedName => $"{Owner.BaseName}_Fn_{OriginalName}";
    public string FullGeneratedName => $"{Owner.Module.FullName}::{GeneratedName}";
    public RustStruct Owner { get; }
    public string? Signature { get; }
    public uint? VirtualIndex { get; }
    private readonly MethodInfo _clrMethod;

    public RustFunction(RustStruct owner, MethodInfo method)
    {
        _clrMethod = method;

        Owner = owner;
        Name = RustTypeRef.SafeSnakeCase(method.Name);
        OriginalName = method.Name;

        Signature = method.GetCustomAttribute<MemberFunctionAttribute>()?.Signature;
        VirtualIndex = method.GetCustomAttribute<VirtualFunctionAttribute>()?.Index;
    }

    public void Export(StringBuilder builder, int indentLevel)
    {
        builder.AppendLine($"{Exporter.Indent(indentLevel)}pub struct {GeneratedName};");

        // Addressable & AddressableMut
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel)}static mut RESOLVED_{GeneratedName}: Option<*const usize> = None;");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}impl crate::Addressable for {GeneratedName} {{");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}fn address() -> Option<*const usize> {{");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}unsafe {{ RESOLVED_{GeneratedName}.clone() }}");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}impl crate::AddressableMut for {GeneratedName} {{");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}fn set_address(resolved: &Option<*const usize>) {{");
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel + 2)}unsafe {{ RESOLVED_{GeneratedName} = resolved.clone(); }}");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");

        // ResolvableMemberFunction (currently a marker trait, but may change in the future)
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel)}impl crate::ResolvableMemberFunction for {GeneratedName} {{}}");

        // SignatureResolvableMemberFunction
        if (Signature != null)
        {
            builder.AppendLine(
                $"{Exporter.Indent(indentLevel)}impl crate::SignatureResolvableMemberFunction for {GeneratedName} {{");
            builder.AppendLine(
                $"{Exporter.Indent(indentLevel + 1)}const SIGNATURE: crate::MemberFunctionSignature = crate::MemberFunctionSignature::new(\"{Signature}\");");
            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
        }

        // VirtualResolvableMemberFunction
        if (VirtualIndex != null && Owner.VTableSignature != null)
        {
            builder.AppendLine(
                $"{Exporter.Indent(indentLevel)}impl crate::VirtualResolvableMemberFunction for {GeneratedName} {{");
            builder.AppendLine(
                $"{Exporter.Indent(indentLevel + 1)}const VIRTUAL_INDEX: usize = {VirtualIndex};");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}fn vtable_address() -> Option<*const usize> {{");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}use crate::Addressable;");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}{Owner.Name}::address()");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
        }

        // make the function callable
        builder.AppendLine($"{Exporter.Indent(indentLevel)}impl {GeneratedName} {{");
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel + 1)}pub unsafe fn call({string.Join(", ", BuildParams(true, true))}) -> {RustTypeRef.FromClrType(_clrMethod.ReturnType)} {{");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}use crate::Addressable;");
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel + 2)}let address = Self::address().expect(\"unresolved function: {GeneratedName}\");");
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel + 2)}let func: extern \"C\" fn({string.Join(", ", BuildParams(false, true))}) -> {RustTypeRef.FromClrType(_clrMethod.ReturnType)} = std::mem::transmute(address);");
        builder.AppendLine(
            $"{Exporter.Indent(indentLevel + 2)}func({string.Join(", ", BuildParams(true, false))})");
        builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
    }

    private List<string> BuildParams(bool withNames, bool withTypes)
    {
        var rustParams = new List<string>();

        if (!_clrMethod.IsStatic)
        {
            if (withNames && withTypes)
            {
                rustParams.Add("&mut self");
            } else if (withNames)
            {
                rustParams.Add("self as *mut Self");
            } else if (withTypes)
            {
                rustParams.Add("*mut Self");
            }
        }

        foreach (var param in _clrMethod.GetParameters())
        {
            var paramName = RustTypeRef.SafeSnakeCase(param.Name!);
            var paramType = RustTypeRef.FromClrType(param.ParameterType);
            if (withNames && withTypes)
            {
                rustParams.Add($"{paramName}: {paramType}");
            }
            else if (withNames)
            {
                rustParams.Add(paramName);
            }
            else if (withTypes)
            {
                rustParams.Add(paramType.ToString());
            }
        }

        return rustParams;
    }

    public static RustFunction FromBaseMethod(RustFunction baseMethod, RustStruct owner)
    {
        var method = new RustFunction(owner, baseMethod._clrMethod);
        return method;
    }
}