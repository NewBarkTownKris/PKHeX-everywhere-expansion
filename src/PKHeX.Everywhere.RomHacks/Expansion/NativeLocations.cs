using System.Globalization;
using System.Text;
using PKHeX.Core;

namespace PKHeX.Everywhere.RomHacks.Expansion;

public readonly record struct NativeOriginMapping(GameVersion Origin, string Region, ushort ExactLocation);

public static class NativeLocations
{
    private static readonly ComboItem[] JohtoKanto = GameInfo.GetLocationList(GameVersion.SS, EntityContext.Gen4, false).Where(x => x.Value is >= 126 and <= 234).ToArray();
    private static readonly ComboItem[] Hoenn = GameInfo.GetLocationList(GameVersion.OR, EntityContext.Gen6, false).Where(x => x.Value is >= 170 and <= 354).ToArray();

    private static string Key(string? name) => string.Concat((name ?? "").Normalize(NormalizationForm.FormD).Where(char.IsLetterOrDigit)).ToUpperInvariant();

    public static NativeOriginMapping Resolve(NativePokemon source)
    {
        string id = source.MetLocation.ToString(CultureInfo.InvariantCulture);
        string name = source.Profile.Locations.GetValueOrDefault(id) ?? "";
        string key = Key(name);
        key = key switch { "TINTOWER" => "BELLTOWER", "OLIVINELIGHTHOUSE" => "LIGHTHOUSE", _ => key };
        string? region = source.Profile.LocationRegions.GetValueOrDefault(id);
        var johto = JohtoKanto.Where(x => Key(x.Text) == key).ToArray();
        var hoenn = Hoenn.Where(x => Key(x.Text) == key).ToArray();
        bool remake = source.Profile.Region == "johto-kanto";
        if (region is "johto" or "kanto" || region != "hoenn" && johto.Length == 1 && (remake || hoenn.Length == 0))
        {
            ushort exact = johto.Length == 1 ? (ushort)johto[0].Value : (ushort)0;
            region ??= IsKanto(exact) ? "kanto" : "johto";
            GameVersion origin = source.Profile.Id.StartsWith("heart-and-soul", StringComparison.Ordinal) ? GameVersion.HG : GameVersion.SS;
            return new(origin, region, exact);
        }
        if (region == "hoenn" || hoenn.Length == 1)
            return new(GameVersion.OR, "hoenn", hoenn.Length == 1 ? (ushort)hoenn[0].Value : (ushort)0);
        return new(remake ? GameVersion.SS : GameVersion.OR, "unknown", 0);
    }

    private static bool IsKanto(ushort location) => location is >= 138 and <= 176 or >= 197 and <= 201 or 203 or 221 or 223 or 224;

    public static ushort PortableLocation(NativeOriginMapping mapping, PKM target)
    {
        if (target is PK4 || mapping.Region == "hoenn" && mapping.ExactLocation != 0) return mapping.ExactLocation;
        string label = mapping.Region switch { "johto" => "the Johto region", "kanto" => "the Kanto region", "hoenn" => "the Hoenn region", _ => "" };
        if (label.Length == 0) return 0;
        var candidates = GameInfo.GetLocationList(GameVersion.OR, EntityContext.Gen6, false).Concat(GameInfo.GetLocationList(GameVersion.SL, EntityContext.Gen9, false));
        return (ushort)candidates.First(x => x.Value >= 30000 && x.Text == label && GameInfo.GetLocationName(false, (ushort)x.Value, target.Format, target.Generation, target.Version) == label).Value;
    }

    public static string DisplayLocation(PKM target) => GameInfo.GetLocationName(false, target.MetLocation, target.Format, target.Generation, target.Version);
}
