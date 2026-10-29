# PKHeX Expansion Web

A working fork of PKHeX Everywhere 0.31.0 for versioned expansion save profiles.

Stock pokeemerald-expansion 1.17.1 (untouched release defaults) has a separate
source-derived profile and synthetic save checks. Other expansion versions or
save-changing configuration options require matching profiles; this is not
universal compatibility with every expansion-based build. Native species balance
for stock/HnS/Soulgold currently falls back to PKHeX data and is marked approximate.

Rocket Hoenn has completed-save and actual-game verification. Heart & Soul 2.0.6
and Soulgold 1.2.1 have source-derived adapters and synthetic tests; real-save
validation is still required. This is a prototype, not a universal hack detector.

Hosted preview: https://newbarktownkris.github.io

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
The first hosting target is an account Pages site or a custom domain at `/`.

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

## Origin locations

Native place names are shown from the game profile. PK6 can store HG/SS as the
origin, but its normal met-location namespace does not retain HG/SS city IDs.
The collection archive preserves the exact native IDs/names and raw records.
Portable files may need an unknown location or a shiny PID adjustment, recorded
in the metadata. PK4 can name Cherrygrove for an explicit HG/SS-style conversion
when its species/moves fit Gen 4; the default does not rewrite origin history.

## Credits and license

Based on [PKHeX Everywhere](https://github.com/arleypadua/PKHeX.Everywhere),
[PKHeX](https://github.com/kwsch/PKHeX), and the supplied game sources.
[PKForge](https://github.com/sofianeelhor/PKForge) was an adapter/conversion design
reference. Preserve upstream credits and GPL-3.0-or-later licensing. Dependency
commits are pinned in `UPSTREAM-SUBMODULES.json`; originals are in `external/`.
See `UPSTREAM-README.md` and `LICENSE`.
