# Emerald Imperium

**Status:** inherited experimental 1.x save adapter. The detector recognizes a distinct 2.0 checksum layout but the loader deliberately refuses it. References: [ImperiumFormat](../../src/PKHeX.Everywhere.RomHacks/Expansion/Imperium/ImperiumFormat.cs), [ImperiumSave](../../src/PKHeX.Everywhere.RomHacks/Expansion/Imperium/ImperiumSave.cs), [ImperiumPokemon](../../src/PKHeX.Everywhere.RomHacks/Expansion/Imperium/ImperiumPokemon.cs).

## Save and mappings

The 1.x implementation uses **one rotated slot of 28 × 4 KiB sectors**, not two ordinary 14-sector slots. Sector IDs and `0x08012025` signature identify fragments; per-ID checksum lengths are `2572, 4084, 4084, 4084, 2152`, then twelve zero-length entries, eight 4084-byte chunks, 1472, 0, 0. Storage starts at sector ID 17 and has 14 × 30 boxed slots. Detection verifies each ID, signature and checksum before loading. The different 2.0 length array is recognized to prevent accidental interpretation, not to assert editing support.

Imperium uses expansion Pokémon representation and game-specific species, item, move and ball tables. Inspect [ExpansionPokemon](../../src/PKHeX.Everywhere.RomHacks/Expansion/ExpansionPokemon.cs) and Imperium's data tables: ball order is profile/game-specific and shiny status uses its odds XOR shiny modifier. Native met location is retained by the adapter; the newer `NativePortable` source-profile origin/region policy should not be assumed for this older inherited adapter. Unmapped/fake species and unsupported IDs need explicit handling and native preservation.

## Evidence

Inherited tests and code provide experimental coverage; confirm the exact ROM release and real save before enabling a new version. [PKHeX Everywhere](https://github.com/arleypadua/PKHeX.Everywhere) supplies the inherited adapter and application; retain its licenses and attribution.
