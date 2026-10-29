# GBA battery saves (.sav / .srm)

**Status:** supported for the specific adapters and versions listed in [the inventory](../../SUPPORTED_FORMATS.md). A file extension is not a game or version identifier. The web app's native expansion and Quetzal paths expect the relevant 128 KiB layout; the local historic investigation also considers 64 KiB data. An `.srm` is useful only when its bytes match a supported raw save; emulator wrappers are not automatically understood.

## Structure and identification

Generation III flash saves commonly rotate logical sections through two physical save slots. Each 4 KiB sector has a section ID, checksum, signature and save counter near its end. [NativeSave.cs](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativeSave.cs) reconstructs the selected slot and verifies profile-specific section lengths and checksums. Never assume that matching the `0x08012025` signature identifies a particular game: several unrelated formats share it. Layout, offsets, field widths, storage and mapping tables are configuration-dependent. SoulGold has additional storage sectors and checks; see [its note](../romhacks/soulgold.md).

Native Pokémon records contain encrypted data in ordinary expansion profiles. Profile-derived bitfields interpret the decrypted data; [Quetzal](../romhacks/quetzal.md) and the inherited [CFRU adapters](../romhacks/unbound.md) use different layouts. See [conversion](conversion.md) for ball, shiny and location rules rather than applying standard Emerald IDs blindly.

## Evidence and limits

See [verification](../../VERIFICATION.md) and the individual profile notes for real versus synthetic coverage. Checksums establish internal consistency, not correct species mapping or retail legality. Preserve the original save and never write recovered Pokémon into a different save layout without an explicit conversion. mGBA snapshots are a separate [container](mgba-state.md); their live RAM can differ from the battery save.

## Historic Pokémon My Ass exporter

The local next build has a separate read-only exporter for the exact supplied Pokémon My Ass revision, using the FireRed-sector reader and the same conservative source mapping as its VBA state adapter. A fresh game-generated starter save validates the party path; PC coverage is synthetic, and other FireRed hacks are not automatically supported. It accepts a 128 KiB image with mGBA's 16-byte RTC footer, retains native records/metadata, and labels PK6 field approximations. See [the game note](../romhacks/pokemon-my-ass.md).
