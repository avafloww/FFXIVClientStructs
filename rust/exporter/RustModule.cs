using System.Text;

namespace RustExporter;

public class RustModule : IRustExportable
{
    public RustModule? Parent { get; }
    public string Name { get; }
    protected readonly Dictionary<string, IRustExportable> _members = new();

    public string FullName => Parent == null ? Name : $"{Parent.FullName}::{Name}";

    public RustModule(RustModule? parent, string name)
    {
        Parent = parent;
        Name = name;
    }

    public RustModule GetOrAddModule(string name)
    {
        if (_members.TryGetValue(name, out var member))
        {
            if (member is RustModule module)
            {
                return module;
            }

            throw new Exception($"Member {name} is not a module");
        }
        else
        {
            var module = new RustModule(this, name);
            _members.Add(name, module);
            return module;
        }
    }

    public void Add(string name, IRustExportable rs)
    {
        if (!_members.ContainsKey(name))
        {
            _members.Add(name, rs);
        }
    }

    private bool IsEffectivelyEmpty => _members.Count == 0 || _members.All(x => x.Value is RustModule
    {
        IsEffectivelyEmpty: true
    });

    public IEnumerable<RustStruct> GetAllStructsRecursive()
    {
        foreach (var member in _members)
        {
            if (member.Value is RustStruct rs)
            {
                yield return rs;
            }
            else if (member.Value is RustModule rm)
            {
                foreach (var subMember in rm.GetAllStructsRecursive())
                {
                    yield return subMember;
                }
            }
        }
    }

    public virtual void Export(StringBuilder builder, int indentLevel)
    {
        if (IsEffectivelyEmpty)
        {
            return;
        }

        var name = RustTypeRef.SafeSnakeCase(Name);

        builder.AppendLine($"{Exporter.Indent(indentLevel)}pub mod {name} {{");
        foreach (var member in _members)
        {
            member.Value.Export(builder, indentLevel + 1);
        }

        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
    }
}