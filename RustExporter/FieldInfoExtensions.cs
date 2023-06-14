using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RustExporter;

public static class FieldInfoExtensions
{
    public static bool IsFixed(this FieldInfo finfo)
    {
        var attr = finfo.GetCustomAttributes(typeof(FixedBufferAttribute), false).Cast<FixedBufferAttribute>()
            .FirstOrDefault();
        return attr != null;
    }

    public static Type GetFixedType(this FieldInfo finfo)
    {
        return finfo.GetCustomAttributes(typeof(FixedBufferAttribute), false).Cast<FixedBufferAttribute>().Single()
            .ElementType;
    }

    public static int GetFixedSize(this FieldInfo finfo)
    {
        return finfo.GetCustomAttributes(typeof(FixedBufferAttribute), false).Cast<FixedBufferAttribute>().Single()
            .Length;
    }

    public static int GetFieldOffset(this FieldInfo finfo)
    {
        var attrs = finfo.GetCustomAttributes(typeof(FieldOffsetAttribute), false);
        if (attrs.Length != 0)
            return attrs.Cast<FieldOffsetAttribute>().Single().Value;

        // Lets assume this is because it's a LayoutKind.Sequential struct
        return Marshal.OffsetOf(finfo.DeclaringType, finfo.Name).ToInt32();
    }
}