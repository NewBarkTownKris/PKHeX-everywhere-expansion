#!/usr/bin/env python3
"""Extract names and numeric Pokémon data from the user's Quetzal 9.1 ROM."""
import hashlib
import json
import struct
import sys
from pathlib import Path

import argparse
root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('rom', type=Path)
parser.add_argument('pokedaisy_scripts', type=Path)
parser.add_argument('--output', type=Path, default=root / 'src/PKHeX.Everywhere.RomHacks/Quetzal/quetzal-9-v1.json')
args = parser.parse_args()
sys.path.insert(0, str(args.pokedaisy_scripts.resolve()))
import gen_quetzal_tables as quetzal

rom = args.rom.read_bytes()
sha = hashlib.sha1(rom).hexdigest()
if sha != 'c108cec750a35775084c0806cc704d8abd1994e7':
    raise SystemExit('This extractor is pinned to English Alpha 9 v1.')
header = lambda offset: struct.unpack_from('<I', rom, 0x100 + offset)[0]
names, info, moves = header(0x44), header(0xBC), header(0x48)
species = []
for i in range(quetzal.SPECIES_COUNT):
    start = info - 0x08000000 + i * 36
    row = rom[start:start + 36]
    species.append({'id': i, 'name': quetzal.text(rom, names + i * 13, 13),
                    'stats': list(row[:6]), 'types': list(row[6:8]), 'gender': row[18],
                    'abilities': list(struct.unpack_from('<3H', row, 24)),
                    'national': struct.unpack_from('<H', rom, 0x54A59A + 2 * (i - 1))[0] if i else 0})
data = {'source': 'Quetzal English Alpha 9 v1', 'romSha1': sha, 'species': species,
        'moves': [quetzal.text(rom, moves + i * 17, 17) for i in range(936)],
        'items': [quetzal.text(rom, 0x091EFDC4 + i * 20, 20) for i in range(890)],
        'abilities': [quetzal.text(rom, 0x0853B1B6 + i * 17, 17) for i in range(400)]}
output = args.output
output.write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':')))
print(f'Wrote {len(species)} species and {len(data["moves"])} move records to {output}.')
