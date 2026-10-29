# Supported games and file formats

This is the compatibility inventory for the **next local build**. ROM-hack support is experimental and depends on the exact game version, configuration and source layout. A valid filename does not make incompatible data compatible. Keep a backup of your original file and review conversions.

## Original games and inherited ROM-hack adapters

The supplied Emerald3DS `.sav` was identified as a standard Emerald battery save and loaded in the supplied Emerald ROM; the other supplied `.sav` was blank. It uses the upstream official Emerald reader.

The upstream [PKHeX Everywhere](https://github.com/arleypadua/PKHeX.Everywhere) application and [PKHeX Core](https://github.com/kwsch/PKHeX) provide support for many official Pokémon games and their compatible save and Pokémon files. Available editing features vary by game and file.

| ROM hack | In-game save support | Notes |
| --- | --- | --- |
| Pokémon Unbound | Experimental save editor | Inherited adapter. Game-specific data and legality support have limits. |
| Pokémon Radical Red | Experimental save editor | Inherited adapter; select the format when detection is uncertain. |
| Emerald Imperium | Experimental save editor | Inherited adapter; the distinct 2.0 layout is detected but deliberately refused. |
| Pokémon Emerald Legacy | Experimental save editor | Inherited adapter; select the format when detection is uncertain. |
| Pokémon Heart & Soul 2.0.6 | Experimental source-profile save editor | Source-derived layout; a randomizer-labeled save passed structural and no-op checks, and an edited copy loaded in the standard 2.0.6 build. The actual randomizer ROM/settings are unavailable, so randomized game-data mappings remain unverified. Other versions require a matching profile. |
| Pokémon SoulGold 1.2.1 | Experimental source-profile save editor | Compact BX21 records and 21 boxes. A supplied save passed no-op and nickname-edit checks, and the edited copy was verified in-game; storage past box 14 has synthetic coverage. Other revisions require their own support. |
| pokeemerald-expansion 1.17.1 | Experimental source-profile save editor | Untouched release defaults; synthetic checks only. Configuration and other versions may change the layout. |
| Rocket Hoenn | Source-profile save editor | Matching project build; checked against a completed save and native game data. |
| Pokémon Quetzal English Alpha 9.1 | Read-only Pokémon export | Separate Quetzal flow; 31-byte box records or an empty PC are supported. Other compact box formats are detected and refused. Individual PK9 files or a ZIP can be exported. |

Polished Crystal 3.2.3 has experimental selected battery and exact normal-ROM mGBA live-party PK6/PK9 recovery, with source-defined checksums, real base species only and documented PID/IV approximations. Pokémon Petalburg Dynasty 1.10.0 has experimental selected battery and exact-build mGBA party/PC PK6/PK9 recovery for recognized real base species; the custom species in the supplied save is skipped. Pokémon Recharged Yellow 1.9.7 has experimental selected battery and exact-build mGBA party/PC recovery; all six supplied records matched live state and produced PK6 files. Pokémon Lazarus 2.0 has experimental selected battery and exact-build mGBA party/PC recovery to PK6/PK9. Both supplied saves were compared against live game states; only recognized real species and mapped moves are exported, with documented assumptions and skipped forms.

The supplied older Rijon Adventures revision has experimental selected battery and exact-build mGBA state recovery. Its party was checked against a file reconstructed from the supplied VBA state and a seeded mGBA Pokémon; no game-created battery save or real occupied PC has been verified. Limited VBA state recovery (player-party records and conditional PK6 exports; no in-game save editing or PC extraction) is available in the next local build for the supplied **Rijon Adventures** and **Rijon Adventures 2009 Beta**, **Pokémon My Ass**, and **Liquid Ocean Beta 1** revisions. Pokémon My Ass, Thunder Emblem Beta 1, Shiny Gold Beta 5 X, Lightning Yellow Demo, Gary Edition Beta 1, Liquid Ocean Beta 1 and Crystal Shards Beta 1 also have experimental read-only battery-save and exact-build mGBA exporters: party checked against freshly generated saves, PC checked synthetically. Shiny Gold New Bark Town and Lightning Yellow and Gary Edition Pallet Town records map to Johto and Kanto regional PK6 origins respectively; other native locations remain unknown. Pokémon Citrine has experimental selected-save and exact-build mGBA live-party PK2 export: a seeded Pikachu in a game-created save was checked; PC tests are synthetic and held items are omitted. Pokémon Cobalt Blue US also has experimental selected-save PK2 export: its fresh game-generated party was checked, PC coverage is synthetic, and held items are removed from portable files but retained in native records. These are tracked separately from the save editors above.

The supplied 2012 Liquid Crystal and 2013 Dark Rising revisions also have experimental, explicitly selected, read-only VBA 9 party/PC export, complete FireRed battery-save export, and exact-build mGBA party/PC export to approximate PK6. The mGBA party was checked against fresh in-game starter saves; mGBA PC has synthetic coverage. Twelve supplied states and a matching Dark Rising battery snapshot were checked. The supplied Liquid Crystal `.sa1` lacks its trainer sector; read-only party/PC extraction is supported and its 60 records match the newest state. The file cannot be edited as a complete save. Held items are dropped and non-Poké Balls replaced; location, shiny, ability and growth assumptions are detailed in the research notes.

Pokémon Heroes, Resolute 1.17 Fixed, Pokémon Isthmis, Snakewood Fixed (real species only), Old White 1.2, Liquid Ocean Alpha 1.2, Pokémon Shattered Dreams, Pokémon Crono Beta 1, LeafGreen USA (supplied build), Pokémon Marble Beta 2.1, Ruby Destiny: Reign of Legends, Pokémon Topaz, Pokémon Ambar (both supplied revisions), Pokémon Naranja (both supplied Ruby revisions), Ruby Destiny: Rescue Rangers Beta 3.2, Omega Beta 1, Rijon Adventures 2009 Beta, Genesis Fixed Beta 3, Jasper Demo, the distinct older Liquid Crystal Beta 1, Orange Generation, Shining Opal Beta 2.13 and Twilight have **experimental local selected battery and exact-build mGBA PK6 export**. Each game displayed and re-saved one Pokémon seeded from a different game; its party record and PK6 match. These are not native-capture fixtures, and PC tests are synthetic. Individual research notes list CRCs and mapping restrictions. VBA states for these titles remain unverified.

Some adapters were inherited from upstream; source profiles and Quetzal support are additions in this fork. Listed games do not imply that every version or every field has been validated. Imported compatible source-profile JSON files last for the browser session. Cross-profile imports require compatible mappings and are not generally enabled.

### Quetzal conversion details

The exporter approximates the original trainer and PID for boxed Pokémon using the save trainer and a generated PID, and labels those approximations. Met locations are suggested Scarlet/Violet encounter locations where known, not the original Quetzal location. Experience, box PP and met level can be approximated. Radiant Pokémon become standard shinies; unsupported or custom balls become Great Balls. Original records and approximation flags are kept in downloadable metadata. Unsupported data is reported rather than guessed.

## File formats

| File | Support in the next local build |
| --- | --- |
| In-game battery saves, commonly `.sav` or `.srm` | Supported for the compatible games and layouts above. Extension alone is not a guarantee. The expansion adapters expect a 128 KiB GBA save, with supported RTC footers when present; Quetzal expects 128 KiB. Official-game support depends on PKHeX's recognized formats. |
| Individual Pokémon files, including `.pk1`, `.pk2`, `.pk3`, `.pk4`, `.pk6` and `.pk9` | Import/export availability depends on the open save and Pokémon compatibility. Expansion collection ZIPs contain portable Pokémon, native `.expmon` records and metadata; Quetzal exports PK9. Cross-profile conversion is restricted. |
| Source-profile `.json` | Imported through **Load ROM Hack → Import source profile** for compatible expansion layout families; session-local. It is not a save file. |
| mGBA GBA save states, raw `.state` and screenshot-style `.ss0` / PNG states | **Local next-build feature, not yet published.** Read-only party/PC recovery for the exact Rocket Hoenn, vanilla expansion, Heart & Soul and SoulGold builds and the exact ROM revisions listed above with mGBA support, including Petalburg Dynasty 1.10.0, Lazarus 2.0, Recharged Yellow 1.9.7, older Rijon, Dark Rising and Liquid Crystal. Evidence varies by game; follow the linked research notes. Other CRCs are rejected. GBA state version 7 only. |
| mGBA Game Boy states, raw or PNG `.ss1` | Read-only live-party recovery for the exact supplied Shin Pokémon Red (PK1), Cobalt Blue and Citrine (PK2), and Polished Crystal 3.2.3 normal ROM (PK6/PK9). PC state extraction and unverified CRCs are not supported. |
| VisualBoyAdvance `.sgm` / `.sg1` states | **Local next-build, limited party recovery and conditional PK6 export:** VBA 8/9 for the supplied Rijon Adventures revisions, Pokémon My Ass and Liquid Ocean Beta 1. The user must select the matching revision; shared ROM headers cannot prove identity. Only source species with an exact official name and standard internal ID are retained. PK6 conversion uses name-based mappings and explicitly labeled approximations; some records are refused. Liquid Crystal and Dark Rising VBA 9 states also include PC extraction; no general VBA support. |
| `.sa1` | An extension alone does not identify the contents. Compatible raw battery saves are handled by their selected reader, including the supplied partial Liquid Crystal Flash dump for read-only party/PC recovery. Blank or incompatible files cannot be recovered by renaming. |
| `.gba` ROM images | Not opened by the editor as saves. Matching ROM/source data can be used offline when developing and validating new profiles. |
| Collection `.zip` | Downloaded export archive with Pokémon, metadata and, where available, native records; not a save file to reopen directly. |

## Intentionally excluded custom-species games

| Game and revision | Why it is excluded |
| --- | --- |
| Pokémon Quartz, supplied full-release ROM (`953bcebf3610…`) | The included author documentation describes a custom 386-species roster, and the supplied state contains custom Pokémon. The project supports real Pokémon only. Reusing a standard Ruby species ID would misidentify a custom Pokémon as an unrelated real species. A future revision shown to contain real Pokémon could be evaluated separately. |

A mixed-roster hack is not automatically excluded: compatible real Pokémon may be exported when their game-specific mappings are verified, while custom and uncertain species are skipped. Other unlisted games remain unverified; do not describe them as custom-species exclusions without evidence.

## Work in progress, not supported in the web app

The specific Liquid Crystal and Dark Rising revisions have limited local export described above; other revisions are unverified. Other mGBA builds and other emulators require verified adapters. The remaining newly supplied ROM hacks and other Game Boy revisions are not yet supported; a game title or base header is not enough to establish mappings.

## Compatibility maintenance checklist

For every change that adds, removes or changes a game, version, file format, adapter, conversion or validation status:

1. Update this inventory and the in-app **Supported formats** page in the same change. Distinguish released capabilities from local or planned work.
2. State the exact supported version/build, input and output types, read/write or export-only behavior, and known restrictions or approximations. Do not imply compatibility based on filename or shared game title alone.
3. Update the README summary, relevant release notes and the planned GitHub About description if their claims change. The About description is short; this inventory carries the full detail.
4. Record the evidence: real-save or game tests, synthetic-only coverage, and unresolved compatibility questions. Update or add meaningful regression checks where necessary.
5. Keep the original PKHeX Everywhere, PKHeX and other upstream credit, license and copyright notices intact; add credits when a new source or dependency is used.

6. Create or update the corresponding hack and file-format notes in [research](research/README.md), including remapping, evidence, uncertainties, code references and attribution. Add new notes to the research index.
7. When a hack or revision is excluded because it uses custom species, record the specific reason and evidence in the exclusion table, its research note, and the report. Distinguish entirely excluded games from mixed rosters and from games that remain unverified for other reasons.
