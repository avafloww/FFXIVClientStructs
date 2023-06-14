using System.Text;

namespace RustExporter;

public abstract class RustTypeDecl : IRustExportable
{
    private static readonly Dictionary<string, RustTypeDecl> TypeRegistry = new();
    // keep track of types that are Copy-tainted (aka, do not derive Copy, or touch something that doesn't derive Copy)
    private static readonly HashSet<string> CopyTaintedTypes = new();
    
    public string Name { get; protected set; }
    public string BaseName => Exporter.GetBaseName(Name);
    public bool IsCopyTainted => CopyTaintedTypes.Contains(Name);
    public RustModule Module { get; }

    public RustTypeDecl(string name, RustModule module)
    {
        Name = name;
        Module = module;
        
        TypeRegistry.Add(name, this);
    }

    public virtual int MarkAsCopyTainted(RustTypeRef? cause = null)
    {
        return CopyTaintedTypes.Add(Name) ? 1 : 0;
    }

    public static RustTypeDecl Get(string rustName)
    {
        if (TypeRegistry.TryGetValue(RustTypeRef.RustNameWithoutGeneric(rustName), out var decl))
            return decl;
        
        throw new Exception($"Type with Rust name {RustTypeRef.RustNameWithoutGeneric(rustName)} not found");
    }

    public static void EnsureForClrType(Type type)
    {
        if (type.IsPrimitive || type.IsFixedBuffer()) return;
        
        var target = type;
        if (type.IsPointer)
        {
            var elemType = type.GetElementType();
            while (elemType.IsPointer) elemType = elemType.GetElementType();
            target = elemType;
        }

        var fullTypeName = RustTypeRef.ClrToRustName(target, true);
        
        if (TypeRegistry.ContainsKey(fullTypeName))
            return;
        
        if (type.IsEnum) TypeRegistry[fullTypeName] = new RustEnum(type);
        else TypeRegistry[fullTypeName] = new RustStruct(type);
    }
    
    public abstract void Export(StringBuilder builder, int indentLevel);
}