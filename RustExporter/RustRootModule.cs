using System.Text;

namespace RustExporter;

public class RustRootModule : RustModule
{
    public static readonly RustRootModule Instance = new();

    public void ProcessCopyTaints()
    {
        int tainted;
        for (var i = 0;; i++)
        {
            tainted = GetAllStructsRecursive()
                .Where(rs => rs.TypeRefs.Any(typeRef => typeRef.Declaration.IsCopyTainted))
                .Sum(rs => rs.MarkAsCopyTainted());

            Console.WriteLine($"ProcessCopyTaints (pass {i}): {tainted} newly copy-tainted structs");
            if (i > 0 && tainted == 0)
            {
                break;
            }
        }
    }
    
    public override void Export(StringBuilder builder, int indentLevel)
    {
        foreach (var member in _members)
        {
            member.Value.Export(builder, indentLevel);
        }
    }
}