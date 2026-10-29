using PKHeX.Core;
using PKHeX.Everywhere.RomHacks.Cfru;
using static System.Buffers.Binary.BinaryPrimitives;
namespace PKHeX.Everywhere.RomHacks.Expansion;

public sealed class NativeBag : PlayerBag
{
    private readonly CfruItemStorage _info;
    public override IItemStorage Info => _info;
    public override IReadOnlyList<CfruPouch> Pouches { get; }
    internal NativeBag(NativeSave save)
    {
        var p = save.Profile; var pocketNames = new[] { ("items", InventoryType.Items), ("keyItems", InventoryType.KeyItems), ("pokeBalls", InventoryType.Balls), ("TMsHMs", InventoryType.TMHMs), ("berries", InventoryType.Berries), ("medicine", InventoryType.Medicine) };
        var known = new Dictionary<ushort, InventoryType>();
        foreach (var (name, type) in pocketNames.Where(x => p.Layout.ContainsKey("Bag." + x.Item1)))
        {
            int at = p.At("SaveBlock1.bag") + p.At("Bag." + name), count = p.At("Bag.count." + name);
            for (int i = 0; i < count; i++) { ushort raw = ReadUInt16LittleEndian(save.Block1[(at + i * 4)..]); ushort modern = p.Items.ToModern(raw); if (modern != 0) known[modern] = type; }
        }
        _info = new CfruItemStorage(p.Items.Items, known);
        Pouches = pocketNames.Where(x => p.Layout.ContainsKey("Bag." + x.Item1)).Select(x => new CfruPouch(x.Item2, Info, 999, p.Items, (ushort)save.EncryptionKey, (p.At("SaveBlock1.bag") + p.At("Bag." + x.Item1), p.At("Bag.count." + x.Item1)))).ToArray();
        foreach (var pouch in Pouches) pouch.GetPouch(save.Block1);
    }
    public override void CopyTo(SaveFile sav) { if (sav is not NativeSave native) throw new InvalidOperationException(); foreach (var pouch in Pouches) pouch.SetPouch(native.Block1); }
}
