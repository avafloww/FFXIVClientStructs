using System.Text;

namespace RustExporter;

public class RustStruct : RustTypeDecl
{
    private const string DERIVE_CLONE = "#[derive(Clone)]";
    private const string DERIVE_COPY_CLONE = "#[derive(Copy, Clone)]";

    // keep track of types that are Copy-tainted (aka, do not derive Copy, or touch something that doesn't derive Copy)
    // this is a little silly because it doesn't account for fullName, but it's probably good enough for now
    private static readonly HashSet<string> CopyTaintedTypes = new();
    
    public string Name { get; }
    public int Size { get; }
    public bool IsUnion { get; set; }
    private readonly List<string> _members = new();
    // todo: fix this, temp hack just while we fix all the other shit
    private string _derive = DERIVE_CLONE; // DERIVE_COPY_CLONE;

    public RustStruct(string name, int size, bool isUnion = false)
    {
        Name = name;
        Size = size;
        IsUnion = isUnion;
    }

    public RustStruct(Type clrType)
    {
        
    }
    
    public void Add(string name, RustTypeRef rustTypeRef, string? prefixComment = null, string? suffixComment = null)
    {
        var sb = new StringBuilder();

        if (prefixComment != null)
        {
            sb.Append("/* ");
            sb.Append(prefixComment);
            sb.Append("*/ ");
        }

        sb.Append(name);
        sb.Append(": ");
        sb.Append(rustTypeRef);
        
        if (suffixComment != null)
        {
            sb.Append(" /* ");
            sb.Append(suffixComment);
            sb.Append("*/");
        }
        
        Add(sb.ToString());
    }
    
    private void Add(string member)
    {
        _members.Add(member);

        if (member.Contains("cpp_std::Deque")
            || member.Contains("cpp_std::Map")
            || member.Contains("cpp_std::Set")
            || member.Contains("cpp_std::Vector"))
        {
            // these cpp_std types don't implement Copy, so we can't derive Copy for the struct
            _derive = DERIVE_CLONE;
        }

        if (member.Contains("_union_") || member.Contains("hk"))
        {
            // handles references where the type is a union type that contains a reference to a non-copy std type
            // also, assume the worst for lolhavok
            // todo: this is a hack, fix this
            _derive = DERIVE_CLONE;
        }
    }
    
    public override void Export(StringBuilder builder, int indentLevel)
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
        builder.AppendLine($"{Exporter.Indent(indentLevel)}{_derive}");
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