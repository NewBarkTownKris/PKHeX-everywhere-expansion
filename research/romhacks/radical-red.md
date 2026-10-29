# Pokémon Radical Red

**Status:** inherited experimental CFRU save adapter, exact game-version coverage not established in this note. Core references: [RadicalRedFormat](../../src/PKHeX.Everywhere.RomHacks/Cfru/RadicalRed/RadicalRedFormat.cs), [RadicalRedSave](../../src/PKHeX.Everywhere.RomHacks/Cfru/RadicalRed/RadicalRedSave.cs), [shared CFRU save](../../src/PKHeX.Everywhere.RomHacks/Cfru/CfruSave.cs), [record codec](../../src/PKHeX.Everywhere.RomHacks/Cfru/CfruPokemon.cs).

## Identification and records

Radical Red shares FireRed's sector signature `0x08012025`, so the detector is **possible**, not certain. It checks block-0 field `0xAC == 1` and whether the security key at `0xF20` is zero or differs from its copy at `0xAF8`. An ambiguous selection must not silently overwrite an ordinary FireRed save. CFRU has 25 boxes and 58-byte compact PC records, four packed 10-bit moves and a noncontiguous box stream including physical sectors 30/31. The shared reader exposes save checksum validation separately; do not assume its initial slot search alone validates all checksums.

## Mapping, evidence and caveats

This adapter exposes FireRed met-location names because its map follows FireRed, as described by the implementation/test comments; other hacks cannot safely inherit that rule. Native species, move, item and ball indices are translated with Radical Red/CFRU tables, including the explicit native ball order in `CfruPokemon`. Preserve native indices for duplicate or unmapped values. Shiny interpretation follows the inherited record codec, not the expansion modifier or Quetzal flags. The direct source-profile HG/SS/ORAS portable remapping policy is not automatically applied to this inherited adapter.

The repository has inherited [facade tests](../../src/PKHeX.Facade.Tests); inspect them and obtain matching real-game fixtures before declaring any specific release broadly verified. [OpenHome](https://github.com/andrewbenington/OpenHome) is credited for species/item/move data, and [PKHeX Everywhere](https://github.com/arleypadua/PKHeX.Everywhere) for the inherited application/adapters.
