using System.Text;

namespace RustExporter;

public abstract class RustTypeDecl : IRustExportable
{
    private static readonly Dictionary<Type, RustTypeDecl> _typeRegistry = new();
    
    public static RustTypeDecl Get(Type type)
    {
        if (_typeRegistry.TryGetValue(type, out var decl))
            return decl;
        
        if (type.IsEnum)
            return _typeRegistry[type] = new RustEnum(type);
        else if (type.IsValueType)
            return _typeRegistry[type] = new RustStruct(type);
        else
            throw new NotSupportedException($"Unsupported type: {type}");
    }
    
    public abstract void Export(StringBuilder builder, int indentLevel);
}