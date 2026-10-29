using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using PKHeX.Everywhere.RomHacks.Quetzal;

namespace PKHeX.Everywhere.Engine.Host;

[SupportedOSPlatform("browser")]
public static partial class QuetzalExports
{
    [JSExport]
    public static string Export(string inspection) => QuetzalExporter.Export(inspection);
}
