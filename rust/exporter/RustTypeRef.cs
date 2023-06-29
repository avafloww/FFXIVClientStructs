using System.Collections.Immutable;
using System.Text;

namespace RustExporter;

public class RustTypeRef
{
    private const int NotArray = -1;
    private const string CrateRef = "crate";
    private const string StdRef = "std";

    // thanks, copilot :^)
    // yes I know some of these are likely wrong, no I do not care, next question
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
    public int PointerDepth { get; internal set; } = 0; // todo: do we need this?
    public RustModule Module { get; }
    public RustTypeDecl Declaration => RustTypeDecl.Get(Name);

    public RustTypeRef(string rustName) : this(rustName, NotArray)
    {
    }

    public RustTypeRef(string rustName, int arraySize)
    {
        var split = rustName.Split(' ');
        for (var i = 0; i < split.Length; i++)
        {
            if (split[i].StartsWith("*"))
            {
                PointerDepth++;
                if (PointerDepth > 2)
                {
                    // todo: 3-depth pointers exist, re-evaluate this?
                    // throw new Exception("Pointer depth too high: " + rustName);
                }
            }
            else
            {
                break;
            }
        }

        // join the split back together, but start at index PointerDepth
        Name = string.Join(' ', split[PointerDepth..]);
        ArraySize = arraySize;

        // traverse to find the root module and ensure it exists
        var components = Name.Split("<")[0].Split("::");
        RustModule module = RustRootModule.Instance;
        for (var i = 0; i < components.Length - 1; i++)
        {
            if (i == 0)
            {
                if (components[i] == CrateRef) continue;
                if (components[i] == StdRef) break;
            }

            module = module.GetOrAddModule(components[i]);
        }

        Module = module;
    }

    public override string ToString()
    {
        var ptr = "";
        for (var i = 0; i < PointerDepth; i++)
        {
            ptr = "*mut " + ptr;
        }

        return ArraySize >= 0 ? $"{ptr}[{Name}; 0x{ArraySize:X}]" : $"{ptr}{Name}";
    }

    public RustTypeRef Clone(string? nameOverride = null)
    {
        var clone = new RustTypeRef(nameOverride ?? Name, ArraySize)
        {
            PointerDepth = PointerDepth
        };

        return clone;
    }

    public static implicit operator RustTypeRef(RustTypeDecl decl) => new(decl.Name);

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

        // if the name matches any Rust keywords, escape it as a raw identifier
        if (RustKeywords.Contains(built))
        {
            // but if it's "self", suffix it instead
            if (built == "self") built += "_";
            else built = "r#" + built;
        }

        return built;
    }

    public static string RustNameWithoutGeneric(string rustName)
    {
        var index = rustName.IndexOf('<');
        return index == -1 ? rustName : rustName[..index] + "<>";
    }

    public static RustTypeRef FromClrType(Type type)
    {
        return FromClrType(type, type.IsArray ? 1 : NotArray);
    }

    public static RustTypeRef FromClrType(Type type, int arraySize)
    {
        return new(ClrToRustName(type), arraySize);
    }

    public static string ClrToRustName(Type type, bool stripGenericParams = false)
    {
        var primitiveRef = RustPrimitive.ReferenceFor(type);
        return primitiveRef != null ? primitiveRef.ToString() : FixComplexTypeName(type, stripGenericParams);
    }

    private static string FixComplexTypeName(Type type, bool stripGenericParams)
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
                fullName = generic.GetFullNameWithoutGenericArity();
                if (dereferenced.IsNested)
                {
                    fullName += '+' + generic.FullName.Split('+')[1].Split('[')[0];
                }

                fullName += '<';

                if (dereferenced.GenericTypeArguments.Length > 0 && !stripGenericParams)
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
            isPrimitive = true;
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

        return fullName;
    }
}
