namespace RustExporter;

public static class TypeExtensions
{
    public static string GetFullNameWithoutGenericArity(this Type t)
    {
        var name = t.FullName!;
        var index = name.IndexOf('`');
        return index == -1 ? name : name[..index];
    }

    public static string GetNameWithoutGenericArity(this Type t)
    {
        var name = t.Name;
        var index = name.IndexOf('`');
        return index == -1 ? name : name[..index];
    }

    public static bool IsFixedBuffer(this Type type)
    {
        return type.Name.EndsWith("e__FixedBuffer");
    }

    public static bool IsStruct(this Type type)
    {
        return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal);
    }
}