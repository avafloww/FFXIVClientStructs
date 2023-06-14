using System.Text;

namespace RustExporter;

public sealed class RustPrimitive : RustTypeDecl
{
    private static readonly List<RustPrimitive> Primitives = new();

    public Type[] ClrTypes { get; }
    public Type[] ClrPtrTypes { get; }
    public Type[] ClrDoublePtrTypes { get; }

    public RustPrimitive(string rustName, Type? clrType, Type? clrPtrType = null, Type? clrDoublePtrType = null) : base(
        rustName, RustRootModule.Instance)
    {
        ClrTypes = clrType == null ? Array.Empty<Type>() : new[] { clrType };
        ClrPtrTypes = clrPtrType == null ? Array.Empty<Type>() : new[] { clrPtrType };
        ClrDoublePtrTypes = clrDoublePtrType == null ? Array.Empty<Type>() : new[] { clrDoublePtrType };

        Primitives.Add(this);
    }

    public RustPrimitive(string rustName, Type[] clrTypes, Type[]? clrPtrTypes = null, Type[]? clrDoublePtrTypes = null)
        : base(rustName, RustRootModule.Instance)
    {
        ClrTypes = clrTypes;
        ClrPtrTypes = clrPtrTypes ?? Array.Empty<Type>();
        ClrDoublePtrTypes = clrDoublePtrTypes ?? Array.Empty<Type>();

        Primitives.Add(this);
    }

    public RustPrimitive(string rustName, bool isCopyTainted)
        : base(rustName, RustRootModule.Instance)
    {
        ClrTypes = Array.Empty<Type>();
        ClrPtrTypes = Array.Empty<Type>();
        ClrDoublePtrTypes = Array.Empty<Type>();

        Primitives.Add(this);
        if (isCopyTainted) MarkAsCopyTainted();
    }

    public override void Export(StringBuilder builder, int indentLevel)
    {
        // primitives are just aliases to the Rust equivalent
    }

    public static RustTypeRef? ReferenceFor(Type type)
    {
        foreach (var primitive in Primitives)
        {
            var pointerDepth = -1;

            if (primitive.ClrDoublePtrTypes.Contains(type)) pointerDepth = 2;
            else if (primitive.ClrPtrTypes.Contains(type)) pointerDepth = 1;
            else if (primitive.ClrTypes.Contains(type)) pointerDepth = 0;

            if (pointerDepth == -1)
            {
                continue;
            }

            var typeRef = new RustTypeRef(primitive.Name);
            typeRef.PointerDepth = pointerDepth;

            return typeRef;
        }

        return null;
    }
}