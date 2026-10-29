# Pokémon Unbound

**Status:** inherited experimental CFRU save adapter. This note describes the repository implementation; it does not establish compatibility with every Unbound release. Relevant files: [UnboundFormat](../../src/PKHeX.Everywhere.RomHacks/Cfru/Unbound/UnboundFormat.cs), [UnboundSave](../../src/PKHeX.Everywhere.RomHacks/Cfru/Unbound/UnboundSave.cs), [UnboundPokemon](../../src/PKHeX.Everywhere.RomHacks/Cfru/Unbound/UnboundPokemon.cs), [CFRU shared reader](../../src/PKHeX.Everywhere.RomHacks/Cfru/CfruSave.cs), [record codec](../../src/PKHeX.Everywhere.RomHacks/Cfru/CfruPokemon.cs).

## What had to be handled

Recognized sector signatures are `0x01122000`, `0x01121999` and `0x01121998`. CFRU uses two 14-sector slots and 25 × 30 boxes, with a **58-byte compact PC record** expanded into a 100-byte in-memory party-shaped representation. Its box stream spans noncontiguous portions of ordinary blocks and physical sectors 30/31; use the exact `BoxStream` ranges in `CfruSave`, not a simple consecutive 80-byte array. Four 10-bit moves are unpacked from five bytes. The save writes per-block checksums; note that `FindActiveSlot` checks sector IDs/signatures and order coverage, while checksum verification is separately exposed by `ChecksumsValid`.

Species, items and moves have game-specific tables and may have custom IDs; never assume national IDs match internal IDs. The shared CFRU ball-order table maps native indices to PKHeX balls, retaining the native index if the value is unchanged. Met location is byte `0x45`; unlike Radical Red, this adapter exposes numbered locations and locks met-location editing instead of claiming FireRed names. Shiny behavior follows the CFRU Pokémon implementation and its PID/OT-derived fields; the expansion shiny-modifier/Quetzal Radiant logic is not automatically applicable. The inherited adapter is not the `NativePortable` source-profile conversion path.

## Evidence

See source and [facade tests](../../src/PKHeX.Facade.Tests) for inherited fixtures and save behavior. Species/item/move mapping data is credited in the in-app Credits page to [OpenHome](https://github.com/andrewbenington/OpenHome); the underlying application comes from [PKHeX Everywhere](https://github.com/arleypadua/PKHeX.Everywhere). Verify version, checksums, encounter interpretation and portable export independently for any additional release.
