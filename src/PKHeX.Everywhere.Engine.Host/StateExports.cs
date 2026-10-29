using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using PKHeX.Everywhere.RomHacks.Expansion;
using PKHeX.Everywhere.RomHacks.Historic;

namespace PKHeX.Everywhere.Engine.Host;

[SupportedOSPlatform("browser")]
public static partial class StateExports
{
    [JSExport]
    public static string Inspect(string state)
    {
        byte[] input = Convert.FromBase64String(state);
        return MgbaShinRed.Inspect(input) ?? PetalburgRecovery.InspectMgba(input) ?? LazarusRecovery.InspectMgba(input) ?? RechargedRecovery.InspectMgba(input) ?? ClassicRecovered.InspectMgba(input) ?? VbaParty.InspectMgba(input) ?? StateExport.Inspect(input);
    }
    [JSExport]
    public static string InspectVba(string state, string profile) => profile is "liquid-crystal" or "dark-rising" ? ClassicRecovered.InspectVba(Convert.FromBase64String(state), profile) : VbaParty.Inspect(Convert.FromBase64String(state), profile);
    [JSExport]
    public static string InspectHistoricBattery(string save, string profile) => profile == "polished-crystal-3-2-3" ? PolishedRecovery.InspectBattery(Convert.FromBase64String(save)) : profile == "petalburg-1-10" ? PetalburgRecovery.InspectBattery(Convert.FromBase64String(save)) : profile == "lazarus-2" ? LazarusRecovery.InspectBattery(Convert.FromBase64String(save)) : profile == "recharged-yellow" ? RechargedRecovery.InspectBattery(Convert.FromBase64String(save)) : profile is "cobalt-blue" or "citrine" ? GbClassic.Inspect(Convert.FromBase64String(save), profile) : profile is "liquid-crystal" or "dark-rising" ? ClassicRecovered.InspectBattery(Convert.FromBase64String(save), profile) : VbaParty.InspectBattery(Convert.FromBase64String(save), profile);
    [JSExport]
    public static string ArchiveVba(string json)
    {
        using var document = JsonDocument.Parse(json);
        bool gb = document.RootElement.TryGetProperty("sourceType", out var type) && type.GetString() == "Game Boy battery save";
        return Convert.ToBase64String(gb ? StateExport.Archive(json) : VbaParty.Archive(json));
    }
    [JSExport]
    public static string Archive(string json)
    {
        using var document = JsonDocument.Parse(json);
        bool historic = document.RootElement.TryGetProperty("sourceType", out var type) && type.GetString() == "mGBA historic state";
        return Convert.ToBase64String(historic ? VbaParty.Archive(json) : StateExport.Archive(json));
    }
}
