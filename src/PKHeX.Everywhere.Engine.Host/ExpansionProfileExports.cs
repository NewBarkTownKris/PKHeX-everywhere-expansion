using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using PKHeX.Everywhere.Engine;
using PKHeX.Everywhere.RomHacks.Expansion;
using PKHeX.Facade;
namespace PKHeX.Everywhere.Engine.Host;

[SupportedOSPlatform("browser")]
public static partial class ExpansionProfileExports
{
    [JSExport]
    public static string Register(string json)
    {
        var profile=SourceProfile.Parse(json);
        if(SaveFormats.Find(profile.Id)!=null)throw new InvalidDataException("That profile ID is already registered. Give a changed layout a new profile ID.");
        SaveFormats.Register(new NativeFormat(profile));return profile.Id;
    }
    [JSExport]
    public static bool HasNativeSave()=>Session.Current.Game?.SaveFile is NativeSave;
    [JSExport]
    public static string ExportCollection()=>Convert.ToBase64String(NativeCollectionArchive.Export(Session.Current.Game??throw new InvalidOperationException("Open a save first.")));
}
