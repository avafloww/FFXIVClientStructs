using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FFXIVClientStructs.Interop.Attributes;

namespace RustExporter;

public class RustStruct : RustTypeDecl
{
    private const string DERIVE_CLONE = "#[derive(Clone)]";
    private const string DERIVE_COPY_CLONE = "#[derive(Copy, Clone)]";

    // a hack to work around a #[derive(Clone)] issue for some structs
    // could probably fix this properly but I'm convinced it's much more work than it's worth
    private static readonly string[] SpecialCaseNoDerive =
    {
        "crate::ffxiv::client::ui::misc::ItemOrderModule_Union",
        "crate::ffxiv::client::ui::misc::ItemOrderModule",
        "crate::ffxiv::client::system::resource::ResourceGraph_CategoryContainer",
        "crate::ffxiv::client::system::resource::ResourceGraph",
        "crate::ffxiv::component::gui::AtkStage",
        "crate::ffxiv::component::gui::AtkValue",
    };

    public int Size { get; }
    public bool IsUnion { get; }
    public int UnionCount { get; private set; }
    public Type? OriginalClrType { get; }
    public VTableAddressAttribute? VTableSignature { get; }
    public IEnumerable<RustTypeRef> TypeRefs => _members.Select(m => m.TypeRef);
    public List<RustFunction> Functions { get; } = new();

    private readonly List<Member> _members = new();
    private string _derive = DERIVE_COPY_CLONE;

    private RustStruct(string name, int size, bool isUnion = false) : base(name, new RustTypeRef(name).Module)
    {
        Size = size;
        IsUnion = isUnion;
        OriginalClrType = null;
    }

    internal RustStruct(Type clrType) : this(clrType, clrType)
    {
    }

    internal RustStruct(RustTypeRef rustType, Type clrType) : base(rustType.Name, rustType.Module)
    {
        OriginalClrType = clrType;

        if (clrType.IsGenericType)
        {
            if (clrType.ContainsGenericParameters)
            {
                Console.WriteLine("Has " + clrType.GetGenericArguments().Length + " generic params: " + rustType);
                // Generic types are ignored if they cannot be instantiated.
                var gls = new List<string>();
                for (int i = 1; i <= clrType.GetGenericArguments().Length; i++)
                {
                    gls.Add($"T{i}");
                }

                Name = Name.Replace("<>", $"<{string.Join(", ", gls)}>");

                // set up this type and bail early
                Size = 0;
                IsUnion = false;

                foreach (var s in gls)
                {
                    Add($"__phantom_{s.ToLower()}", new RustTypeRef($"std::marker::PhantomData<{s}>"));
                }

                rustType.Module.Add(Name, this);
            }

            return;
        }
        else
        {
            Size = SizeOf(clrType);
        }

        var pad = Size.ToString("X").Length;
        var padFill = new string(' ', pad + 2);

        // export fields
        var offset = -1;
        var fieldGroupings = clrType.GetFields()
            .Where(finfo => !Attribute.IsDefined(finfo, typeof(ObsoleteAttribute)))
            .Where(finfo => !finfo.IsLiteral) // not constants
            .Where(finfo => !finfo.IsStatic) // not static
            .OrderBy(finfo => finfo.GetFieldOffset())
            .GroupBy(finfo => finfo.GetFieldOffset());
        var unionIndex = 0;
        foreach (var grouping in fieldGroupings)
        {
            // first defined field of the class
            if (offset == -1) offset = grouping.Key;
            
            var fieldOffset = grouping.Key;
            var finfos = grouping
                .Where((finfo) => finfo.Name.ToLower() != "vtbl" && finfo.Name.ToLower() != "vtable")
                .OrderByDescending(Exporter.GetEffectiveFieldSize)
                .ToList();

            var unionMaxSize = 0;
            var isUnion = finfos.Count > 1;
            RustStruct rsTarget = this;

            string? unionName = null;
            if (isUnion)
            {
                unionName = GenerateNextUnionName();
                rsTarget = new RustStruct(unionName, 0, true);
                rustType.Module.Add(rsTarget.Name, rsTarget);
            }

            for (int i = 0; i < finfos.Count; i++)
            {
                var finfo = finfos[i];
                if (ShouldSkipField(finfo)) continue;

                var fieldType = finfo.FieldType;
                var fieldSize = Exporter.GetEffectiveFieldSize(finfo);

                if (!isUnion)
                    offset = FillGaps(offset, fieldOffset, padFill);

                if (offset > fieldOffset)
                {
                    Debug.WriteLine(
                        $"Current offset exceeded the next field's offset (0x{offset:X} > 0x{fieldOffset:X}): {rustType}.{finfo.Name}");
                    break;
                }

                RustTypeRef rustTypeRef;
                if (finfo.IsFixed())
                {
                    var fixedType = finfo.GetFixedType();
                    var fixedSize = finfo.GetFixedSize();
                    EnsureForClrType(fixedType);

                    rustTypeRef = new RustTypeRef(RustTypeRef.ClrToRustName(fixedType), fixedSize);
                }
                else
                {
                    rustTypeRef = new RustTypeRef(RustTypeRef.ClrToRustName(fieldType));
                }

                rsTarget.Add(RustTypeRef.SafeSnakeCase(finfo.Name), rustTypeRef,
                    string.Format($"0x{{0:X{pad}}}", offset));

                if (isUnion)
                {
                    unionMaxSize = Math.Max(unionMaxSize, 8);
                    unionMaxSize = Math.Max(unionMaxSize, fieldSize);
                }
                else
                {
                    offset += fieldSize;
                }
            }

            if (!isUnion) continue;
            Add($"_union_0x{offset:x}", new RustTypeRef(unionName!), string.Format($"0x{{0:X{pad}}}", offset));
            offset += unionMaxSize;
        }

        if (offset != -1) FillGaps(offset, Size, padFill);

        // export methods
        var methods = clrType.GetMethods()
            .Where(m => !Attribute.IsDefined(m, typeof(ObsoleteAttribute)));

        // export functions
        foreach (var method in methods.Where(m =>
                     Attribute.IsDefined(m, typeof(MemberFunctionAttribute)) ||
                     Attribute.IsDefined(m, typeof(VirtualFunctionAttribute)) ||
                     Attribute.IsDefined(m, typeof(StaticAddressAttribute))))
        {
            var function = new RustFunction(this, method);
            Functions.Add(function);
        }

        // export vtable information
        VTableSignature = clrType.GetCustomAttribute<VTableAddressAttribute>();

        rustType.Module.Add(Name, this);
    }

    public string GetUnionName(int unionIndex)
    {
        if (IsUnion) throw new Exception("Cannot get union name for union structure!");
        if (unionIndex < 0) throw new ArgumentOutOfRangeException(nameof(unionIndex));

        var unionSuffix = "_Union";
        if (unionIndex > 0)
        {
            unionSuffix += $"_{unionIndex}";
        }

        return Name + unionSuffix;
    }

    private bool ShouldSkipField(FieldInfo info)
    {
        // ugly hack to work around union fuckery in Human
        if (info.DeclaringType == typeof(FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Human))
        {
            return info.Name == "EquipSlotData";
        }

        return false;
    }

    private string GenerateNextUnionName()
    {
        return GetUnionName(UnionCount++);
    }

    public override int MarkAsCopyTainted(RustTypeRef? cause = null)
    {
        if (!IsUnion) _derive = DERIVE_CLONE;

        var tainted = 0;
        for (int i = 0; i < UnionCount; i++)
        {
            tainted += Get(GetUnionName(i)).MarkAsCopyTainted(this);
        }

        // all types must implement Copy in unions, so if this is a union, we need to
        // ensure that all members implement Copy, or if they don't (aka they are copy tainted),
        // wrap them in ManuallyDrop
        if (IsUnion)
        {
            foreach (var member in _members)
            {
                if (member.TypeRef.Name.StartsWith("std::mem::ManuallyDrop")) continue;

                var decl = Get(member.TypeRef.Name);
                if (decl.IsCopyTainted)
                {
                    member.TypeRef = member.TypeRef.Clone($"std::mem::ManuallyDrop<{member.TypeRef.Name}>");
                }
            }
        }

        return base.MarkAsCopyTainted(this) + tainted;
    }

    public void Add(string name, RustTypeRef rustTypeRef, string? prefixComment = null, string? suffixComment = null)
    {
        // ugly hack for AgentContext
        if (rustTypeRef.Name.EndsWith("system::drawing::Point"))
        {
            AddInternal($"{name}_x", new RustTypeRef("i32"), $"hack({name})", suffixComment);
            AddInternal($"{name}_y", new RustTypeRef("i32"), $"hack({name})", suffixComment);
        }
        else
        {
            AddInternal(name, rustTypeRef, prefixComment, suffixComment);
        }
    }

    private void AddInternal(string name, RustTypeRef rustTypeRef, string? prefixComment = null,
        string? suffixComment = null)
    {
        _members.Add(new Member(name, rustTypeRef, prefixComment, suffixComment));
    }

    public override void Export(StringBuilder builder, int indentLevel)
    {
        var type = IsUnion ? "union" : "struct";
        var sizeComment = IsUnion ? "" : $" /* Size=0x{Size:X} */";
        if (Size == 0)
        {
            if (_members.Count > 0 && _members.TrueForAll(m => m.TypeRef.Name.Contains("PhantomData")))
            {
                // this is a generic type with parameters
                sizeComment = " /* Size=unknown (generic type with parameters) */";
            }
        }

        builder.AppendLine($"{Exporter.Indent(indentLevel)}#[repr(C)]");

        // unions with manual-drop members should not derive Copy or Clone
        var shouldDerive = !IsUnion || !_members.Any(m => m.TypeRef.Name.StartsWith("std::mem::ManuallyDrop<"));
        shouldDerive &= !SpecialCaseNoDerive.Contains(Name);
        if (shouldDerive)
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel)}{_derive}");
        }

        if (_members.Count == 0)
        {
            var filler = new RustTypeRef("u8", Size);
            builder.AppendLine($"{Exporter.Indent(indentLevel)}pub {type} {BaseName}({filler});{sizeComment}");
        }
        else
        {
            builder.AppendLine($"{Exporter.Indent(indentLevel)}pub {type} {BaseName} {{{sizeComment}");
            foreach (var member in _members)
            {
                builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}{member},");
            }

            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
        }

        // export vtable info
        if (VTableSignature != null)
        {
            // Addressable & AddressableMut
            builder.AppendLine(
                $"{Exporter.Indent(indentLevel)}static mut RESOLVED_{BaseName}_VT: Option<*const usize> = None;");
            builder.AppendLine($"{Exporter.Indent(indentLevel)}impl crate::Addressable for {BaseName} {{");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}fn address() -> Option<*const usize> {{");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}unsafe {{ RESOLVED_{BaseName}_VT.clone() }}");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
            builder.AppendLine($"{Exporter.Indent(indentLevel)}impl crate::AddressableMut for {BaseName} {{");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}fn set_address(resolved: &Option<*const usize>) {{");
            builder.AppendLine(
                $"{Exporter.Indent(indentLevel + 2)}unsafe {{ RESOLVED_{BaseName}_VT = resolved.clone(); }}");
            builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");

            // ResolvableVTable
            var sig = Exporter.CreateRustSignature(VTableSignature.Signature);
            builder.AppendLine($"{Exporter.Indent(indentLevel)}impl crate::ResolvableVTable for {BaseName} {{");
            builder.AppendLine(
                $"{Exporter.Indent(indentLevel + 1)}const SIGNATURE: crate::VTableSignature = crate::VTableSignature::new({sig}, {VTableSignature.Offset}, {VTableSignature.IsPointer.ToString().ToLowerInvariant()});");
            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
        }

        // export all functions
        if (Functions.Count > 0 && !IsUnion)
        {
            foreach (var function in Functions)
            {
                function.Export(builder, indentLevel);
            }

            builder.AppendLine($"{Exporter.Indent(indentLevel)}impl {BaseName} {{");
            foreach (var function in Functions)
            {
                builder.AppendLine(
                    $"{Exporter.Indent(indentLevel + 1)}pub fn {function.Name}() -> {function.GeneratedName} {{");
                builder.AppendLine($"{Exporter.Indent(indentLevel + 2)}{function.GeneratedName}");
                builder.AppendLine($"{Exporter.Indent(indentLevel + 1)}}}");
            }

            builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
        }
    }

    public int FillGaps(int offset, int maxOffset, string padFill)
    {
        int gap;
        while ((gap = maxOffset - offset) > 0)
        {
            if (offset % 8 == 0 && gap >= 8)
            {
                var gapDiv = gap - (gap % 8);
                Add($"_gap_0x{offset:x}", new RustTypeRef("u8", gapDiv), padFill);
                offset += gapDiv;
            }
            else if (offset % 4 == 0 && gap >= 4)
            {
                Add($"_gap_0x{offset:x}", new RustTypeRef("u8", 4), padFill);
                offset += 4;
            }
            else if (offset % 2 == 0 && gap >= 2)
            {
                Add($"_gap_0x{offset:x}", new RustTypeRef("u8", 2), padFill);
                offset += 2;
            }
            else
            {
                Add($"_gap_0x{offset:x}", new RustTypeRef("u8"), padFill);
                offset += 1;
            }
        }

        return offset;
    }

    public static int SizeOf(Type type)
    {
        // Marshal.SizeOf doesn't work correctly because the assembly is unmarshaled, and more specifically, it sets bools as 4 bytes long...
        return (int?)typeof(Unsafe).GetMethod("SizeOf")?.MakeGenericMethod(type).Invoke(null, null) ?? 0;
    }

    private record Member(string Name, RustTypeRef TypeRef, string? PrefixComment, string? SuffixComment)
    {
        public string Name = Name;
        public RustTypeRef TypeRef = TypeRef;
        public string? PrefixComment = PrefixComment;
        public string? SuffixComment = SuffixComment;

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (PrefixComment != null)
            {
                sb.Append("/* ");
                sb.Append(PrefixComment);
                sb.Append(" */ ");
            }

            sb.Append(Name);
            sb.Append(": ");
            sb.Append(TypeRef);

            if (SuffixComment != null)
            {
                sb.Append(" /* ");
                sb.Append(SuffixComment);
                sb.Append(" */");
            }

            return sb.ToString();
        }
    }
}