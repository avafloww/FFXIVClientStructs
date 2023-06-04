using System.Text;

namespace RustExporter;

public interface IRustExportable
{
    public void Export(StringBuilder builder, int indentLevel);
}