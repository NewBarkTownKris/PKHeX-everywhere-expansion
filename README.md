# PKHeX Expansion Web

A browser-based Pokémon save editor and ROM-hack compatibility project, forked
from [PKHeX Everywhere](https://github.com/arleypadua/PKHeX.Everywhere) by
Arley Padua and its contributors. It builds on [PKHeX](https://github.com/kwsch/PKHeX)
by Kurt and contributors. This fork adds versioned expansion profiles and
ROM-hack export tools; see [Credits and license](#credits-and-license).

The detailed [compatibility and file-format list](SUPPORTED_FORMATS.md) identifies
supported games, versions, export-only features, experimental adapters, and the
local save-state work awaiting release. The website also has a Supported formats
page. Update both whenever supported games, formats, restrictions or validation
status change; see the maintenance checklist in that document.

The opening screen has separate **Open save file** and **Open save state** buttons;
the latter offers the mGBA and VisualBoyAdvance readers. **Export Quetzal Pokémon**
is directly below the regular save button and opens the dedicated Quetzal exporter.

The local next build also offers limited VBA 8/9 **party recovery and conditional PK6 export**
for selected Rijon Adventures, Pokémon My Ass and Liquid Ocean revisions. It
skips species that do not match the source ROM allowlist. PK6 exports use
name-based mappings, standard ability/shiny assumptions, and unknown met
locations; original records and metadata are retained. See the compatibility
list for exact scope. The local next build also supports read-only battery and
exact-build mGBA export for Thunder Emblem Beta 1, Shiny Gold Beta 5 X, Lightning Yellow Demo and Gary Edition Beta 1,
live-party PK1 export for the supplied Shin Pokémon Red mGBA build, and
selected-save and exact-build mGBA live-party PK2 export for Cobalt Blue US. Cobalt Blue
held items are removed from portable files and preserved in native records.
Shiny Gold New Bark and Lightning Yellow/Gary Edition Pallet encounters use Johto/Kanto regional approximations; most historic
locations remain unknown. PC support is synthetic-only for the first two and
not available in Shin Red state export.

Developer-facing [compatibility research](research/README.md) documents each
ROM hack and file format, source layouts, mapping decisions, evidence and
limitations. Update the relevant notes alongside every compatibility change.

Stock pokeemerald-expansion 1.17.1 (untouched release defaults) has a separate
source-derived profile and synthetic save checks. Other expansion versions or
save-changing configuration options require matching profiles; this is not
universal compatibility with every expansion-based build. Native species balance
for stock/HnS/Soulgold currently falls back to PKHeX data and is marked approximate.

Rocket Hoenn has completed-save and actual-game verification. Heart & Soul 2.0.6
and SoulGold 1.2.1 have source-derived adapters and supplied-save edit checks.
Heart & Soul’s built-in randomizer shares its save format; randomized abilities
are calculated in-game and are not currently reflected in portable exports.
This is a prototype, not a universal hack detector.

Hosted preview: https://newbarktownkris.github.io/PKHeX-everywhere-expansion/

## Try the built site

```bash
python3 tools/serve.py
```

Open http://127.0.0.1:8765. Open your save and select its matching profile. The
Save page downloads edited saves and collection archives. Imported profiles are
session-local. Keep the original save separately.

## Build

Install .NET 10, Node 22+, npm and Python 3.12+, then run:

```bash
./tools/build-web.sh
```

The static site is in `src/PKHeX.Web.React/dist/`. It requires no backend. Serve
`.wasm` files as `application/wasm` and use an index.html fallback for app routes.
Set `PKHEX_BASE_PATH=/PKHeX-everywhere-expansion/` when building for this
project’s GitHub Pages site. Local builds default to `/`.

## Generate a profile

With the game's source and ARM toolchain installed:

```bash
python3 tools/generate_profiles.py --repo /path/to/hack --id my-hack-v1 --name "My Hack v1"
```

For HnS builds also pass `--game-define POKEMON_HNS`. Import the resulting JSON
from `profiles/` through Load ROM Hack → Import source profile. A compatible
layout family is required; compact Soulgold BX21 has its own codec. Regeneration
requires the same build configuration as the ROM. The built-in Rocket profile
also reads numeric personal data from its matching local ROM/ELF when generated.

## Checks

```bash
./tools/dotnet.sh build src/PKHeX.Everywhere.ProfileChecks -c Release -m:1 -p:DisableTransitiveFrameworkReferenceDownloads=true
./tools/dotnet.sh run --no-build --project src/PKHeX.Everywhere.ProfileChecks -c Release -- --synthetic-only
npm --prefix src/PKHeX.Web.React test
```

Private completed-save checks take a separate save path and use the earlier
native extraction outside this repository. No user save or ROM is included in
the static build. `.local/` contains ignored tool caches and private QA output.

## Pokémon Quetzal Alpha 9.1

Use **More ways to open → Export Quetzal Pokémon** to open an English Alpha 9.1
save and download individual PK9 files. This is a read-only export flow. It
validates both 16-sector slots and uses the newest complete one. The currently
supported compact box format uses 31-byte records; 21- and 38-byte box formats
are detected and refused until separately validated.

Box records omit their original trainer and PID. The exporter uses the save
trainer and a generated PID, clearly identified in the downloadable metadata.
Experience is adjusted to the retained level, PP is estimated for box records,
and met levels are approximations. It selects a low-level Scarlet/Violet wild
location for the same species and form, preferring Paldea; when none exists the
location is left unknown. These locations are suggestions, not actual encounters.
The Quetzal export screen offers individual PK9 files and a ZIP containing all
supported Pokémon, a metadata report, and notes. Unsupported records are listed
in the metadata with an error rather than silently omitted.
Radiant Pokémon export as normal shinies; their custom palettes cannot be retained
in PK9. Metadata identifies each converted Radiant Pokémon. Quetzal-only and unmapped balls become Great Balls; metadata records the original
ball ID. Some unusual held items can confuse automatic
file format detection; those entries tell you to select PK9 when importing.
Unsupported species, forms, moves or items are reported rather than guessed.

The Quetzal mappings use numeric game data extracted from the user's matching
ROM with [PokeDaisy](https://github.com/lidor30/pokedaisy)’s GPL-3.0 tools. No ROM or save is included. A
real Alpha 9.1 save was checked against the running game and 54 exported Pokémon
were checked with PKHeX Core. Other builds and save configurations require their
own verification.

## Origin locations

Portable Pokémon exports prioritize useful locations. Johto/Kanto records use
HeartGold (Heart & Soul) or SoulSilver origins. PK4 keeps exact HG/SS city/route
names when the Pokémon, form, moves, ability, ball and names fit Gen 4. Modern
data uses PK6/PK9 with “the Johto region” or “the Kanto region” instead. A
collection archive includes modern regional alternatives for its PK4 files.

Hoenn records use Omega Ruby origins and matching OR/AS location names in PK6
or PK9. Unmatched Hoenn areas can use “the Hoenn region”. Unknown custom areas
remain unmapped unless the profile supplies a region; names are not guessed.
Profiles can supply `locationRegions` overrides for ambiguous native places.

These are deliberate origin conversions for ROM-hack records. The archive
records both native and exported origins, locations and PID changes. Native
saves are not rewritten by exporting Pokémon. Nature, ability slot and forced
shininess can require a portable PID change; the original stays in metadata.
These exports are not a claim that a hack encounter is legal in a retail game.

## Credits and license

PKHeX Everywhere by [Arley Padua and contributors](https://github.com/arleypadua/PKHeX.Everywhere)
provides the original application, interface, engine, and inherited ROM-hack
adapters. [PKHeX and PKHeX.Core](https://github.com/kwsch/PKHeX), maintained by
Kurt and contributors, provide the core Pokémon save and file support.

The existing credits page also acknowledges the PKHeX-Plugins maintainers,
OpenHome, PokeAPI/sprites, the PKMN RBYGSC font, and PokeDaisy. The expansion
profiles use matching [pokeemerald-expansion](https://github.com/rh-hideout/pokeemerald-expansion)
and other game sources; [PKForge](https://github.com/sofianeelhor/PKForge) was
an adapter and conversion design reference. The Polished Crystal reader uses the published [Polished Crystal](https://github.com/Rangi42/polishedcrystal) 3.2.3 source and release symbols; PokeDaisy research informed additional game tables and memory layouts. See the in-app Credits page for
links and individual acknowledgments.

The combined project is GPL-3.0-or-later. Preserve upstream copyright notices,
license files and attribution. Dependency commits are pinned in
`UPSTREAM-SUBMODULES.json`; the preserved upstream project description is in
`UPSTREAM-README.md`. See `LICENSE` for the full license.
