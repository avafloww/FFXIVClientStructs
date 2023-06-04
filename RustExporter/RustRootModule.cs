using System.Text;

namespace RustExporter;

public class RustRootModule : RustModule
{
    public override void Export(StringBuilder builder, int indentLevel)
    {
        foreach (var member in _members)
        {
            member.Value.Export(builder, indentLevel);
        }
    }
}