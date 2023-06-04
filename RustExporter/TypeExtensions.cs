namespace RustExporter;

public static class TypeExtensions
{
    public static bool IsFixedBuffer(this Type type)
    {
        return type.Name.EndsWith("e__FixedBuffer");
    }

    public static bool IsStruct(this Type type)
    {
        return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal);
    }
}