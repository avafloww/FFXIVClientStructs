using System.Text;

namespace RustExporter;

public class RustModule : IRustExportable
{
    public string Name { get; init; }
    protected readonly Dictionary<string, IRustExportable> _members = new();
    
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
            var module = new RustModule { Name = name };
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
    
    public virtual void Export(StringBuilder builder, int indentLevel)
    {
        var name = RustTypeRef.SafeSnakeCase(Name);
        
        builder.AppendLine($"{Exporter.Indent(indentLevel)}pub mod {name} {{");
        foreach (var member in _members)
        {
            member.Value.Export(builder, indentLevel + 1);
        }
        builder.AppendLine($"{Exporter.Indent(indentLevel)}}}");
    }
}