using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Cfru;
using PKHeX.Everywhere.RomHacks.Expansion.HeartAndSoul;

namespace PKHeX.Everywhere.RomHacks.Expansion;

public sealed class SourceProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Region { get; set; } = "unknown";
    public Dictionary<string, long> Layout { get; set; } = [];
    public int[] ChecksumLengths { get; set; } = [];
    public Dictionary<string, string> Locations { get; set; } = [];
    public Dictionary<string, string> LocationRegions { get; set; } = [];
    public Dictionary<string, Dictionary<string, int>> Symbols { get; set; } = [];
    public Dictionary<string, int[]> BitFields { get; set; } = [];
    public Dictionary<string, JsonElement> Personal { get; set; } = [];
    internal CfruSpeciesMap Species { get; private set; } = null!;
    internal CfruItemMap Items { get; private set; } = null!;
    internal MoveMap Moves { get; private set; } = null!;
    internal Ball[] Balls { get; private set; } = [];
    internal Dictionary<int, int> Abilities { get; private set; } = [];
    internal Dictionary<int, PersonalInfo> NativePersonal { get; } = [];
    public int At(string name) => unchecked((int)Layout[name]);
    public bool Compact => At("sizeof.BoxPokemon") == 68;
    public static IReadOnlyList<SourceProfile> All { get; } = Load();
    public static SourceProfile Find(string id) => All.Single(p => p.Id == id);
    public static SourceProfile Parse(string json)
    {
        if (json.Length > 8_000_000) throw new InvalidDataException("The profile is too large.");
        var p = JsonSerializer.Deserialize(json, ProfileJsonContext.Default.SourceProfile) ?? throw new InvalidDataException("Empty profile.");
        if (!Regex.IsMatch(p.Id, "^[a-z0-9][a-z0-9-]{0,79}$") || p.Name.Length is < 1 or > 120) throw new InvalidDataException("Invalid profile identity.");
        if (p.At("sizeof.BoxPokemon") is not (80 or 68) || p.At("sizeof.Pokemon") != (p.Compact ? 96 : 100) || p.At("sizeof.SaveBlock1") is < 600 or > 15872 || p.At("sizeof.SaveBlock2") is < 100 or > 3968 || p.At("sizeof.PokemonStorage") is < 100 or > 60000 || p.At("TOTAL_BOXES_COUNT") is < 1 or > 24 || p.At("IN_BOX_COUNT") != 30) throw new InvalidDataException("Unsupported profile dimensions.");
        if (p.ChecksumLengths.Length != 14 || p.ChecksumLengths.Any(n => n < 0 || n > 3968) || p.Locations.Count > 32768 || p.BitFields.Count > 200 || p.BitFields.Values.Any(bits => bits.Length > 32 || bits.Any(b => b < 0 || b >= p.At("sizeof.BoxPokemon") * 8))) throw new InvalidDataException("Invalid profile bounds.");
        foreach (var group in p.Symbols.Values) if (group.Count > 12000 || group.Values.Any(v => v < 0 || v > 65535)) throw new InvalidDataException("Invalid symbol table.");
        p.Initialize(); return p;
    }
    private static string Key(string s) => Regex.Replace(s, "[^a-zA-Z0-9]", "").ToUpperInvariant();

    private static SourceProfile[] Load()
    {
        var asm = typeof(SourceProfile).Assembly;
        return asm.GetManifestResourceNames().Where(n => n.StartsWith("profiles.") && n.EndsWith(".json"))
            .Select(n => { using var stream = asm.GetManifestResourceStream(n)!; var p = JsonSerializer.Deserialize(stream, ProfileJsonContext.Default.SourceProfile)!; p.Initialize(); return p; }).ToArray();
    }
    private Dictionary<int, int> EnumMap<T>(string prefix) where T : struct, Enum
    {
        var names = Enum.GetValues<T>().Distinct().ToDictionary(x => Key(x.ToString()), x => Convert.ToInt32(x));
        return Symbols[prefix].Where(p => names.ContainsKey(Key(p.Key[(prefix.Length + 1)..])))
            .GroupBy(p => p.Value).ToDictionary(g => g.Key, g => names[Key(g.First().Key[(prefix.Length + 1)..])]);
    }
    private void Initialize()
    {
        var normal = EnumMap<Species>("SPECIES");
        var length = Symbols["SPECIES"].Values.Where(v => v < 2048).Max() + 1;
        var entries = new CfruSpecies[length];
        foreach (var (index, national) in normal.Where(p => p.Key < length)) entries[index] = new((ushort)national, 0);
        foreach (var group in Symbols["SPECIES"].Where(p => p.Value > 0 && p.Value < length).GroupBy(p => p.Value))
        {
            int index = group.Key;
            if (entries[index].Species != 0) continue;
            var old = index < HeartAndSoulSpeciesTable.NationalByIndex.Length ? HeartAndSoulSpeciesTable.NationalByIndex[index] : default;
            var baseName = old.Species != 0 ? Key(((Species)old.Species).ToString()) : "";
            if (baseName.Length > 0 && group.Any(x => Key(x.Key[8..]).StartsWith(baseName))) entries[index] = old;
            else entries[index] = new HackSpecies(group.First().Key[8..].Replace('_', ' '));
        }
        Species = new(entries);
        var itemNames = GameInfo.Strings.itemlist.Select((name, index) => (Key: Key(name), Index: index)).Where(x => x.Key.Length > 0).DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Index);
        var itemArray = new ushort[Symbols["ITEM"].Values.Where(v => v < 1024).Max() + 1];
        foreach (var (name, index) in Symbols["ITEM"].Where(x => x.Value < itemArray.Length)) if (itemNames.TryGetValue(Key(name[5..]), out var modern)) itemArray[index] = (ushort)modern;
        Items = new(itemArray);
        var moveMap = EnumMap<Move>("MOVE");
        var moves = new ushort[Symbols["MOVE"].Values.Where(v => v < 2048).Max() + 1];
        foreach (var (index, id) in moveMap.Where(p => p.Key < moves.Length)) moves[index] = (ushort)id;
        Moves = new(moves);
        Balls = new Ball[Symbols["BALL"].Values.Where(v => v < 64).Max() + 1];
        foreach (var (index, id) in EnumMap<Ball>("BALL").Where(p => p.Key < Balls.Length)) Balls[index] = (Ball)id;
        Abilities = EnumMap<Ability>("ABILITY");
        foreach (var (index, element) in Personal)
        {
            int N(string key) => element.GetProperty(key).GetInt32();
            var nativeAbilities = element.GetProperty("abilities").EnumerateArray().Select(v => Abilities.GetValueOrDefault(v.GetInt32())).ToArray();
            var types = element.GetProperty("types").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            static byte Type(int type) => (byte)(type >= 9 ? type - 1 : type);
            NativePersonal[int.Parse(index)] = new PersonalInfo9SV(new byte[PersonalInfo9SV.SIZE])
            {
                HP = N("baseHP"),
                ATK = N("baseAttack"),
                DEF = N("baseDefense"),
                SPE = N("baseSpeed"),
                SPA = N("baseSpAttack"),
                SPD = N("baseSpDefense"),
                Type1 = Type(types[0]),
                Type2 = Type(types[1]),
                Gender = (byte)N("genderRatio"),
                EXPGrowth = (byte)N("growthRate"),
                CatchRate = (byte)N("catchRate"),
                Ability1 = nativeAbilities[0],
                Ability2 = nativeAbilities[1] == 0 ? nativeAbilities[0] : nativeAbilities[1],
                AbilityH = nativeAbilities[2] == 0 ? nativeAbilities[0] : nativeAbilities[2],
                FormCount = 1,
            };
        }
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SourceProfile))]
internal partial class ProfileJsonContext : JsonSerializerContext { }
