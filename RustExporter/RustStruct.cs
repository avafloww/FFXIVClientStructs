using System.Text;

namespace RustExporter;

public class RustStruct : IRustExportable
{
    public string Name { get; }
    public int Size { get; }
    public bool IsUnion { get; set; }
    private readonly List<string> _members = new();

    public RustStruct(string name, int size, bool isUnion = false)
    {
        Name = name;
        Size = size;
        IsUnion = isUnion;
    }
    
    public void Add(string member)
    {
        _members.Add(member);
    }
    
    public void Export(StringBuilder builder, int indentLevel)
    {
        var type = IsUnion ? "union" : "struct";
        var sizeComment = IsUnion ? "" : $" /* Size=0x{Size:X} */";
        if (Size == 0)
        {
            if (_members.TrueForAll(m => m.Contains("PhantomData")))
            {
                // this is a generic type with parameters
                sizeComment = " /* Size=unknown (generic type with parameters) */";
            }
        }
        
        builder.AppendLine($"{Exporter.Indent(indentLevel)}#[repr(C)]");
        builder.AppendLine($"{Exporter.Indent(indentLevel)}#[derive(Copy, Clone)]");
        if (_members.Count == 0)
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel)}pub {type} {Name};{sizeComment}");
        }
        else
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel)}pub {type} {Name} {{{sizeComment}");
            foreach (var member in _members)
            {
                builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}{member},");
            }

            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
        }
    }
}