# Shared expansion conversion policy

**Status:** implemented for versioned `NativePokemon` source profiles; not a universal policy for inherited CFRU/Imperium adapters or Quetzal. Sources: [NativePokemon](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativePokemon.cs), [NativePortable](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativePortable.cs), [NativeLocations](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativeLocations.cs), [archive](../../src/PKHeX.Everywhere.RomHacks/Expansion/NativeCollectionArchive.cs), [location checks](../../src/PKHeX.Everywhere.ProfileChecks/LocationChecks.cs).

## IDs and balls

Profiles contain source symbol tables and compiler-derived field bit positions. The implementation maps named native species, forms, moves, items, abilities and balls to PKHeX identifiers; this is a version/configuration-dependent mapping, not a universal numeric offset. `NativePokemon` exposes the native species/item index separately. Ball indices are interpreted through the profile's `BALL` symbol table and PKHeX's ball enum. Unknowns do not become an arbitrary standard ball in this path; the state exporter refuses a portable conversion when it detects an unmapped nonzero ball, move, item or species. Preserve the native record and report the failure. Quetzal has a **different**, explicitly documented Great Ball fallback.

## Shininess and PID

Native shiny state is `(ShinyXor < profile.SHINY_ODDS) XOR outer.shinyModifier`; do not infer it solely from standard Gen 3 PID odds. Nature can also be modified independently of PID. Native editing changes the modifier to express the intended shiny state without replacing the PID. When a portable format derives shiny, gender, nature or ability slot from PID, `NativePortable` may search for another PID that preserves the supported combination. The output metadata identifies PID changes and retains native records; inability to preserve a required combination is an error. Gen 4's shiny constraint uses XOR < 8, while later contexts differ; always validate the specific target.

## Origins and met locations

`NativeLocations.Resolve` looks up the source location ID and optional `locationRegions`, normalizes names, handles specific aliases (e.g. Tin Tower → Bell Tower), and looks for a unique target location. Johto/Kanto sources map to HG or SS: exact HG/SS locations in PK4 when the data fits, otherwise PK6/PK9 regional labels. Hoenn uses OR with exact OR/AS names when available, otherwise its regional label. Unknown regions remain unknown rather than receiving a fabricated route. These are deliberately changed **portable origins**; they are not claims about the original game or legal encounters. A collection includes a modern regional alternative for a PK4 and records native/exported origins, locations and PID changes.

## Evidence and limits

Location and PID behavior has synthetic checks, plus completed Rocket Hoenn data and native-getter comparisons documented in [verification](../../VERIFICATION.md). Stock expansion, HnS and SoulGold source profiles have synthetic save checks; representative real saves remain necessary. Standard personal data can be a fallback where native source data was not captured. Unsupported formats, custom species/items/moves, eggs and special forms require separate coverage. Review the implementation for current details rather than treating this note as a legality guarantee.
