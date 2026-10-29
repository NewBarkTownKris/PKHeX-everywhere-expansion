# Pokémon Emerald Legacy

**Status:** inherited experimental save adapter. References: [EmeraldLegacyFormat](../../src/PKHeX.Everywhere.RomHacks/Legacy/EmeraldLegacyFormat.cs), [EmeraldLegacySave](../../src/PKHeX.Everywhere.RomHacks/Legacy/EmeraldLegacySave.cs), [block layout](../../src/PKHeX.Everywhere.RomHacks/Legacy/EmeraldLegacyBlockLarge.cs), [bag](../../src/PKHeX.Everywhere.RomHacks/Legacy/EmeraldLegacyBag.cs).

## Identification and behavior

Legacy uses Emerald's sector IDs/signature and can resemble vanilla Emerald. The detector checks three Pokédex-seen copies at Legacy offsets, disagreement at Emerald offsets, and plausible inventory counts using the save's security key; it deliberately reports a **possible** match. It refuses to claim a match when an empty key-item pouch provides no corroboration. Ask the user to choose a format on ambiguity rather than treating ordinary Emerald as Legacy.

This adapter supplies Legacy-specific block, inventory and event-flag/work offsets, including event labels. It inherits standard Gen 3 Pokémon handling where applicable; this investigation has not established a hack-specific ball mapping, forced-shiny scheme or HG/SS/ORAS portable-location remapping. Do not attribute the versioned `NativePortable` conversion policy to it without new evidence.

## Evidence and attribution

See [EmeraldLegacySaveTests](../../src/PKHeX.Facade.Tests/EmeraldLegacySaveTests.cs) and detection comments for fixture-backed behavior. [PKHeX Everywhere](https://github.com/arleypadua/PKHeX.Everywhere) is the upstream application and source of this inherited adapter. Check exact release/configuration and preserve original saves before changing support.
