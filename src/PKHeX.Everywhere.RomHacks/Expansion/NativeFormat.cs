using System.Collections.Frozen;
using PKHeX.Core;
using PKHeX.Facade;
using PKHeX.Facade.Abstractions;
using PKHeX.Facade.Pokemons;
namespace PKHeX.Everywhere.RomHacks.Expansion;

public sealed class NativeFormat(SourceProfile profile) : ISaveFormat
{
    public string Id => profile.Id;
    public string Name => profile.Name;
    public GameVersion BaseGame => GameVersion.E;
    public IReadOnlySet<Capability> Capabilities => FrozenSet<Capability>.Empty;
    public IGameDataSource GameData(SaveFile save) => new NativeGameData(profile);
    public SaveFormatMatch Detect(ReadOnlySpan<byte> data) => NativeSave.TrySlot(profile, data, out _, out _) ? SaveFormatMatch.Possible : SaveFormatMatch.No;
    public SaveFile Load(byte[] data)
    {
        try { return new NativeSave(profile, data); }
        catch (InvalidDataException) { throw new GameNotLoadedException(); }
    }
}

internal sealed class NativeGameData(SourceProfile profile) : IGameDataSource
{
    private static readonly PKHeXGameData Emerald = new(BlankSaveFile.Get(GameVersion.E));
    public IReadOnlyList<Choice> Species => profile.Species.Species.Select(id => new Choice(id, GameInfo.Strings.specieslist[id])).OrderBy(c => c.Name).ToArray();
    public IReadOnlyList<Choice> Items => profile.Items.Items.Select(id => new Choice(id, GameInfo.Strings.itemlist[id])).Prepend(new Choice(0, "None")).ToArray();
    public IReadOnlyList<Choice> HeldItems => profile.Items.HeldItems.Select(id => new Choice(id, GameInfo.Strings.itemlist[id])).OrderBy(c => c.Name).Prepend(new Choice(0, "None")).ToArray();
    public IReadOnlyList<Choice> Moves => profile.Moves.All.Select(id => new Choice(id, GameInfo.Strings.movelist[id])).OrderBy(c => c.Name).ToArray();
    public IReadOnlyList<Choice> Balls => profile.Balls.Where(b => b != Ball.None).Distinct().Select(b => new Choice((int)b, b.ToString() + " Ball")).ToArray();
    public IReadOnlyList<Choice> Natures => Emerald.Natures;
    public IReadOnlyList<Choice> Languages => Emerald.Languages;
    public IReadOnlyList<Choice> OriginGames => Emerald.OriginGames;
    public IReadOnlyList<Choice> MetLocations(GameVersion origin, bool egg = false) => profile.Locations.Select(x => new Choice(int.Parse(x.Key), x.Value)).ToArray();
    public string? NameOf(GameDataKind kind, int id) => kind switch
    {
        GameDataKind.MetLocation => profile.Locations.GetValueOrDefault(id.ToString()) ?? $"Location #{id}",
        GameDataKind.Species => profile.Species.NameOf(id),
        GameDataKind.Move => profile.Moves.NameOf(id),
        GameDataKind.Item or GameDataKind.HeldItem => profile.Items.UnknownIndex(id) is { } i ? profile.Symbols["ITEM"].FirstOrDefault(p => p.Value == i).Key?[5..].Replace('_', ' ') ?? $"Item #{i}" : null,
        _ => null,
    };
    public int ItemIndex(int item) => profile.Items.ToIndex((ushort)item) ?? 0;
    public IReadOnlySet<PokemonField> Locked { get; } = new[] { PokemonField.Gender }.ToFrozenSet();
    public bool StatsApproximate => profile.NativePersonal.Count == 0;
    public bool StoresAbilitySlot => true;
}
