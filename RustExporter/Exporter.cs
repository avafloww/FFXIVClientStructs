using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace RustExporter;

public class Exporter
{
    public static string Indent(int level)
    {
        return new string(' ', level * 4);
    }

    #region singleton

    private static Exporter _exporter;
    public static Exporter Instance => _exporter ??= new Exporter();

    #endregion

    public static readonly string FFXIVNamespacePrefix = string.Join(".",
        new string[] { nameof(FFXIVClientStructs), nameof(FFXIVClientStructs.FFXIV), "" });

    public static readonly string HavokNamespacePrefix = string.Join(".",
        new string[] { nameof(FFXIVClientStructs), nameof(FFXIVClientStructs.Havok), "" });

    public static readonly string STDNamespacePrefix = string.Join(".",
        new string[] { nameof(FFXIVClientStructs), nameof(FFXIVClientStructs.STD), "" });

    public static readonly string InteropNamespacePrefix = string.Join(".",
        new string[] { nameof(FFXIVClientStructs), nameof(FFXIVClientStructs.Interop), "" });

    private Type[] GetExportableTypes(string assemblyName)
    {
        var assembly = AppDomain.CurrentDomain.Load(assemblyName);

        Type[] definedTypes;
        try
        {
            definedTypes = assembly.DefinedTypes.Select(ti => ti.AsType()).ToArray();
        }
        catch (ReflectionTypeLoadException ex)
        {
            definedTypes = ex.Types.Where(t => t != null).ToArray();
        }

        return definedTypes
            .Where(t => t.FullName.StartsWith(FFXIVNamespacePrefix) || t.FullName.StartsWith(HavokNamespacePrefix))
            .Where(t => t.IsEnum || t.StructLayoutAttribute?.Value != LayoutKind.Auto)
            .ToArray();
    }

    public string Export()
    {
        PopulatePrimitives();

        var header = new StringBuilder();
        var definedTypes = GetExportableTypes(nameof(FFXIVClientStructs));

        foreach (var type in definedTypes)
        {
            RustTypeDecl.EnsureForClrType(type);
        }

        RustRootModule.Instance.ProcessCopyTaints();
        RustRootModule.Instance.ProcessVirtualFunctions();

        RustRootModule.Instance.Export(header, 0);

        return header.ToString();
    }

#pragma warning disable CA1806
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    private void PopulatePrimitives()
    {
        new RustPrimitive("()", typeof(void)); // probably a return value (or lack thereof)
        new RustPrimitive("std::ffi::c_void", null, typeof(void*), typeof(void**));
        new RustPrimitive("i8",
            new[] { typeof(char), typeof(sbyte) },
            new[] { typeof(char*), typeof(sbyte*) },
            new[] { typeof(char**), typeof(sbyte**) }
        );
        new RustPrimitive("bool", typeof(bool), typeof(bool*), typeof(bool**));
        new RustPrimitive("f32", typeof(float), typeof(float*));
        new RustPrimitive("f64", typeof(double));
        new RustPrimitive("i16", typeof(short), typeof(short*));
        new RustPrimitive("i32", typeof(int), typeof(int*));
        new RustPrimitive("i64", typeof(long));
        new RustPrimitive("u8", typeof(byte), typeof(byte*), typeof(byte**));
        new RustPrimitive("u16", typeof(ushort), typeof(ushort*));
        new RustPrimitive("u32", typeof(uint), typeof(uint*));
        new RustPrimitive("u64", typeof(ulong));
        new RustPrimitive("usize", null, typeof(IntPtr)); // todo?

        // objectively not primitives, but count as primitives for our purposes
        new RustPrimitive("std::marker::PhantomData<>", false);
        new RustPrimitive("std::mem::ManuallyDrop<>", false);

        // these cpp_std types don't implement Copy, so mark them as copy-tainted
        new RustPrimitive("crate::cpp_std::Map<>", true);
        new RustPrimitive("crate::cpp_std::Deque<>", true);
        new RustPrimitive("crate::cpp_std::Vector<>", true);
        new RustPrimitive("crate::cpp_std::Set<>", true);
        
        // other cpp_std types that we provide
        new RustPrimitive("crate::cpp_std::Pair<>", false);
        new RustPrimitive("crate::cpp_std::String", false);
    }
#pragma warning restore CA1806

    public static int GetEffectiveFieldSize(FieldInfo finfo)
    {
        if (finfo.IsFixed())
        {
            var fixedType = finfo.GetFixedType();
            var fixedSize = finfo.GetFixedSize();
            return RustStruct.SizeOf(fixedType) * fixedSize;
        }

        if (finfo.FieldType.IsPointer) return 8;
        if (finfo.FieldType.IsEnum) return RustStruct.SizeOf(Enum.GetUnderlyingType(finfo.FieldType));
        if (finfo.FieldType.IsGenericType) return Marshal.SizeOf(Activator.CreateInstance(finfo.FieldType));

        return RustStruct.SizeOf(finfo.FieldType);
    }

    public static string GetBaseName(string name)
    {
        var parts = name.Split("::");
        return parts[^1];
    }
}