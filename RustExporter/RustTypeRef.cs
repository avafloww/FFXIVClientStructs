using System.Collections.Immutable;
using System.Text;

namespace RustExporter;

public class RustTypeRef
{
    private const int NotArray = -1;
    
    private static readonly ImmutableHashSet<string> RustKeywords = ImmutableHashSet.Create(new[]
    {
        "as", "break", "const", "continue", "crate", "else", "enum", "extern", "false", "fn", "for", "if", "impl",
        "in", "let", "loop", "match", "mod", "move", "mut", "pub", "ref", "return", "self", "Self", "static",
        "struct", "super", "trait", "true", "type", "unsafe", "use", "where", "while", "async", "await", "dyn",
        "abstract", "become", "box", "do", "final", "macro", "override", "priv", "typeof", "unsized", "virtual",
        "yield", "try"
    });
    
    public string Name { get; }
    public string BaseName => Name.Split("::")[^1];
    public int ArraySize { get; } = NotArray;

    public RustTypeRef(string name)
    {
        Name = name;
    }
    
    public RustTypeRef(string name, int arraySize)
    {
        Name = name;
        ArraySize = arraySize;
    }
    
    public override string ToString()
    {
        return ArraySize >= 0 ? $"[{Name}; 0x{ArraySize:X}]" : Name;
    }

    public static implicit operator RustTypeRef(RustStruct rs) => new(rs.Name);
    public static implicit operator RustTypeRef(RustEnum re) => new(re.Name);
    
    public static implicit operator RustTypeRef(Type clrType) => FromClrType(clrType);

    public static string SafeSnakeCase(string input)
    {
        // generally converts UpperCamelCase to snake_case, except for subsequent uppercase letters

        var sb = new StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && char.IsUpper(input[i - 1]))
                {
                    sb.Append(char.ToLower(c));
                }
                else
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLower(c));
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        var built = sb.ToString();

        // if the name matches any Rust keywords, suffix it with an underscore
        if (RustKeywords.Contains(built))
        {
            built += "_";
        }

        return built;
    }

    public static RustTypeRef FromClrType(Type type)
    {
        return FromClrType(type, type.IsArray ? 1 : NotArray);
    }
    
    public static RustTypeRef FromClrType(Type type, int arraySize)
    {
        return new(ClrToRustName(type), NotArray);
    }
    
    public static string ClrToRustName(Type type)
    {
        if (type == typeof(void))
        {
            // probably a return value
            return "()";
        }

        if (type == typeof(void*)) return "*mut std::ffi::c_void";
        if (type == typeof(void**)) return "*mut *mut std::ffi::c_void";
        if (type == typeof(char) || type == typeof(byte) || type == typeof(sbyte)) return "i8";
        if (type == typeof(char*) || type == typeof(byte*)) return "*mut i8";
        if (type == typeof(char**) || type == typeof(byte**)) return "*mut *mut i8";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(float)) return "f32";
        if (type == typeof(double)) return "f64";
        if (type == typeof(short)) return "i16";
        if (type == typeof(int)) return "i32";
        if (type == typeof(long)) return "i64";
        if (type == typeof(ushort)) return "u16";
        if (type == typeof(uint)) return "u32";
        if (type == typeof(ulong)) return "u64";
        if (type == typeof(IntPtr)) return "*mut usize"; // todo?
        if (type == typeof(short*)) return "*mut i16";
        if (type == typeof(ushort*)) return "*mut u16";
        if (type == typeof(int*)) return "*mut i32";
        if (type == typeof(uint*)) return "*mut u32";
        if (type == typeof(Single*)) return "*mut f32";

        return FixComplexTypeName(type);
    }
    
    private static string FixComplexTypeName(Type type)
    {
        string fullName;
        var isPrimitive = false;
        if (type.IsGenericType || (type.IsPointer && type.GetElementType().IsGenericType))
        {
            
            // todo: fix generics
            bool isPointer = type.IsPointer;
            var dereferenced = isPointer ? type.GetElementType() : type;
            var generic = dereferenced.GetGenericTypeDefinition();
            
            if (type.FullName.StartsWith(typeof(FFXIVClientStructs.Interop.Pointer<>).FullName))
            {
                fullName = ClrToRustName(dereferenced.GenericTypeArguments[0]) + '*';
                isPrimitive = true; // todo this won't always be correct, probably need to refactor
            }
            else
            {
                fullName = generic.FullName.Split('`')[0];
                // if (isPointer) fullName = "*mut " + fullName;
                if (dereferenced.IsNested)
                {
                    fullName += '+' + generic.FullName.Split('+')[1].Split('[')[0];
                }

                fullName += '<';

                if (dereferenced.GenericTypeArguments.Length > 0)
                {
                    var fixedNames = new List<string>();
                    foreach (var argType in dereferenced.GenericTypeArguments)
                    {
                        fixedNames.Add(ClrToRustName(argType));
                    }

                    fullName += string.Join(", ", fixedNames);
                }

                fullName += '>';
            }
        }
        else
        {
            fullName = type.FullName;
        }

        // Console.WriteLine("FULL: " + fullName);
        if (fullName.StartsWith(Exporter.FFXIVNamespacePrefix))
        {
            fullName = fullName.Remove(0, Exporter.FFXIVNamespacePrefix.Length);
            fullName = "ffxiv." + fullName;
        }

        if (fullName.StartsWith(Exporter.HavokNamespacePrefix))
        {
            fullName = fullName.Remove(0, Exporter.HavokNamespacePrefix.Length);
            fullName = "havok." + fullName;
        }
        
        if (fullName.StartsWith(Exporter.STDNamespacePrefix))
        {
            // Console.WriteLine("STD:  " + fullName);
            fullName = fullName.Remove(0, Exporter.STDNamespacePrefix.Length);
            if (fullName.StartsWith("Std"))
            {
                fullName = fullName.Remove(0, 3);
            }

            fullName = "cpp_std." + fullName;
        }

        if (fullName.StartsWith(Exporter.InteropNamespacePrefix))
            fullName = fullName.Remove(0, Exporter.InteropNamespacePrefix.Length);

        if (fullName.Contains("FFXIVClientStructs, Version"))
        {
            if (fullName.EndsWith("*"))
            {
                fullName = "*mut std::ffi::c_void";
            }
            else
            {
                throw new Exception($"Failed to fix name: {fullName}");
            }
        }

        // snake-case all components except for the last one
        var parts = fullName.Split('.');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            parts[i] = RustTypeRef.SafeSnakeCase(parts[i]);
        }

        fullName = string.Join(".", parts);
        fullName = fullName.Replace(".", "::").Replace("+", "_");
        if (!isPrimitive) fullName = "crate::" + fullName;

        // execute twice to account for double-pointers
        for (var i = 0; i < 2; i++)
        {
            if (fullName.EndsWith('*'))
            {
                fullName = string.Concat("*mut ", fullName.AsSpan(0, fullName.Length - 1));
            }
        }
        
        if (type.IsPointer)
        {
            fullName = "*mut " + fullName;
        }

        return fullName;
    }
}