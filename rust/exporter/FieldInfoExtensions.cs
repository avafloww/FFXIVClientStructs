using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFXIVClientStructs;

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
        {
            return attrs.Cast<FieldOffsetAttribute>().Single().Value;
        }

        // Marshal.OffsetOf is broken because it returns 4 bytes for a bool
        // We'll just implement it ourselves, I guess
        var fields = finfo.DeclaringType!.GetFields()
            .Where(finfo => !Attribute.IsDefined(finfo, typeof(ObsoleteAttribute)))
            .Where(finfo => !Attribute.IsDefined(finfo, typeof(CExportIgnoreAttribute)))
            .Where(finfo => !finfo.IsLiteral) // not constants
            .Where(finfo => !finfo.IsStatic);
        
        var offset = 0;
        // iterate through all fields before this one and add their sizes
        foreach (var field in fields)
        {
            if (field == finfo)
            {
                break;
            }

            if (field.FieldType == typeof(bool))
            {
                offset += 1;
            }
            else
            {
                offset += Exporter.GetEffectiveFieldSize(field);
            }
            
        }

        // Lets assume this is because it's a LayoutKind.Sequential struct
        return offset;
    }
}
