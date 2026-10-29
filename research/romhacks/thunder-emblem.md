# Pokémon Thunder Emblem Beta 1 — local compatibility

## Scope and identification

Read-only extraction from a GBA battery save selected explicitly as Thunder Emblem Beta 1, and from mGBA GBA states with ROM CRC `6A2B8EE2` (supplied ROM SHA-256 prefix `5707932ae1b8`). A FireRed header is not unique to this hack: battery saves cannot prove identity, so users must select the matching revision. Other ROM revisions and VBA states are unverified.

## Evidence and layout

A fresh game in the supplied ROM yielded a level 5 Pikachu, saved normally, and resumed from that save in mGBA. A native save reader and the live mGBA state reader found the same checksum-valid party Pokémon and PID. Conditional PK6 exports from both preserved species, nickname, trainer and moves in the sample; the output's met location is unknown. The party and PC reader follows the FireRed sector and record layout, and the mGBA live memory locations were checked against this game-created state. Occupied-PC extraction has synthetic coverage only; no game-created Thunder Emblem PC occupant was tested. Code: `Historic/VbaParty.cs`, `Historic/HistoricGen3.cs` and `Historic/VbaPortable.cs`; synthetic regression checks: `PKHeX.Everywhere.ProfileChecks/VbaChecks.cs`. Original player files are never included in the repository.

## Mapping and limits

Candidate species mapping is restricted to 358 exact official species-name and standard Gen 3 internal-ID matches from this ROM. Other IDs are skipped, including custom or uncertain species. This name match alone does not prove all custom form or mechanic semantics. Candidate move and item tables use exact source-name matches (350 moves and 228 item names); mismatches cause export refusal. Only a standard Poké Ball is accepted for PK6; shiny follows the ordinary Gen 3 PID rule and ability conversion follows standard species data, both assumptions not verified across this hack. Party level is native; PC level is calculated using standard growth. Original trainer and PID are retained when possible, and source records and original met IDs are included in metadata. Unknown location remains `(None)` rather than pretending the ROM's locations are retail Kanto. Eggs, invalid records and unsupported mappings are refused. No claim of retail legality.

The game sample covers one real species, one ordinary Poké Ball, and its initial moves; other real Pokémon, shiny behavior, special forms, occupied PC and edited saves require independent tests. Do not publish ROMs, state files, saves or extracted player records.
