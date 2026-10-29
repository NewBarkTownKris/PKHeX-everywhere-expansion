#!/usr/bin/env python3
"""Regenerate the HnS 2.0.6 randomizer tables from a matching source-built ROM.

Usage: python tools/generate_hns_randomizer_data.py /path/to/hns-build
The build directory must contain the source, pokehns.elf and pokehns.gba.
"""
import json
import re
import struct
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
build = Path(sys.argv[1]).resolve()
profile = json.loads((root / 'profiles/heart-and-soul-2.0.6.json').read_text())
text = (build / 'src/data/randomizer/ability_whitelist.h').read_text()
names = re.findall(r'\bABILITY_[A-Z0-9_]+\b', text[text.index('static const'):text.index('#define ABILITY_WHITELIST_SIZE')])
abilities = profile['symbols']['ABILITY']
whitelist = [abilities[name] for name in names]
symbols = subprocess.check_output(['arm-none-eabi-nm', '-S', str(build / 'pokehns.elf')], text=True)
match = re.search(r'^([0-9a-fA-F]+) ([0-9a-fA-F]+) [A-Za-z] gSpeciesInfo$', symbols, re.M)
if not match:
    raise SystemExit('gSpeciesInfo symbol not found')
start, length = (int(x, 16) for x in match.groups())
start -= 0x08000000
layout = profile['layout']
size, offset = layout['sizeof.SpeciesInfo'], layout['SpeciesInfo.abilities']
rom = (build / 'pokehns.gba').read_bytes()
indices = sorted({i for i in profile['symbols']['SPECIES'].values() if 0 <= i < 2048})
if layout['sizeof.Ability'] != 2 or max(indices) * size + offset + 6 > length or start < 0 or start + length > len(rom):
    raise SystemExit('ROM and profile species layouts disagree')
species = {str(i): list(struct.unpack_from('<HHH', rom, start + i * size + offset)) for i in indices}
result = {'source': 'Heart & Soul 2.0.6 source-built gSpeciesInfo and randomizer ability whitelist', 'whitelist': whitelist, 'speciesAbilities': species}
path = root / 'src/PKHeX.Everywhere.RomHacks/Expansion/HeartAndSoul/randomizer-abilities.json'
path.write_text(json.dumps(result, separators=(',', ':')) + '\n')
print(f'Wrote {len(species)} species and {len(whitelist)} whitelist entries to {path}')
