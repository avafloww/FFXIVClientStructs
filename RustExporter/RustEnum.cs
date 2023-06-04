using System.Text;

namespace RustExporter;

public class RustEnum : IRustExportable
{
    public string Name { get; }
    public string UnderlyingType { get; }
    private readonly Dictionary<string, string> _members = new();
    
    public RustEnum(string name, string underlyingType)
    {
        Name = name;
        UnderlyingType = underlyingType;
    }
    
    public void Add(string name, string value)
    {
        _members.Add(name, value);
    }
    
    public void Export(StringBuilder builder, int indentLevel)
    {
        builder.AppendLine($"{Exporter.Indent(indentLevel)}#[repr({UnderlyingType})]");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}#[derive(Copy, Clone, Debug)]");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}pub enum {Name} {{");
        foreach (var member in _members)
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}{member.Key} = {member.Value},");
        }
        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
    }
}