using System.Text;

namespace RustExporter;

public class RustEnum : RustTypeDecl
{
    public string UnderlyingType { get; }
    private readonly Dictionary<string, string> _members = new();

    internal RustEnum(Type clrType) : this(clrType, clrType)
    {
    }

    internal RustEnum(RustTypeRef rustType, Type clrType) : base(rustType.Name, rustType.Module)
    {
        UnderlyingType = RustTypeRef.ClrToRustName(clrType.GetEnumUnderlyingType());

        try
        {
            var values = Enum.GetValues(clrType);
            for (int i = 0; i < values.Length; i++)
            {
                var value = values.GetValue(i)!;
                var name = Enum.GetName(clrType, value)!;
                Add(name, $"{value:D}");
            }
        }
        catch
        {
            // ignored for now (due to hkArrayFlags)
        }

        Module.Add(Name, this);
    }

    public void Add(string name, string value)
    {
        _members.Add(name, value);
    }

    public override void Export(StringBuilder builder, int indentLevel)
    {
        if (_members.Count > 0)
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel)}#[repr({UnderlyingType})]");
        }

        builder.AppendLine($"{Exporter.Indent(indentLevel)}#[derive(Copy, Clone, Debug)]");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}pub enum {BaseName} {{");
        foreach (var member in _members)
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}{member.Key} = {member.Value},");
        }

        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
    }
}