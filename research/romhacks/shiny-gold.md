# Pokémon Shiny Gold Beta 5 X — local compatibility

## Scope and identification

The supplied ROM (SHA-256 prefix `a642b33a3b7d`, mGBA CRC `61EF78C2`) has experimental read-only extraction from a user-selected GBA battery save and exact-build mGBA state. A generic FireRed header cannot establish revision identity for battery saves; choose the matching game. VBA states and other versions are not validated.

## Evidence

A new game in the supplied ROM received Cyndaquil in Professor Elm's lab in New Bark Town, saved normally and reloaded in mGBA. The battery and live-state readers recovered the same party Pokémon and PID; generated PK6 files reopened with the same species, trainer and starter moves. Game-created PC Pokémon, shinies, nonstarter species and edited saves remain untested. Synthetic tests exercise an occupied PC slot, wrong ROM rejection and the regional mapping. Code: `Historic/VbaParty.cs`, `Historic/VbaPortable.cs`; checks: `ProfileChecks/VbaChecks.cs`. No user save, state or ROM is included.

## Mapping and limitations

The source ROM species-name table has 386 matches to official Gen 3 species names and internal IDs; only these exact matches are retained. This is a conservative necessary check, not proof that all forms, stats or behaviors are unchanged. The source move/item name comparison has 350 move and 218 item matches; unrecognized fields are refused. PK6 accepts only the verified standard Poké Ball, assumes standard PID shiny behavior and ability semantics, preserves the trainer and source record, and calculates boxed levels using standard growth. Egg export is refused; source records and metadata retain original values.

The real Cyndaquil received at New Bark has native met ID 143; the ROM's map-name table independently labels 143 `NEW BARK TOWN`. For that ID only, PK6 uses HeartGold origin and the Generation 6-compatible **Johto region** met label. This is a deliberate regional approximation because PK6 cannot retain the exact HG/SS location as a local origin location in this workflow; the original ID is retained in metadata. Other location IDs currently export as unknown rather than being misreported as FireRed places. Gen 3 origin, altered PID if needed to preserve standard shiny state, and source move/ball/item IDs are documented in the archive metadata. Species/move/item name matching cannot establish legality or game-specific effect equivalence.
