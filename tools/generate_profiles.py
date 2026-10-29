#!/usr/bin/env python3
"""Generate layout profiles with the game's ARM compiler, without building a ROM."""
from pathlib import Path
import argparse, hashlib, json, re, struct, subprocess

ROOT=Path(__file__).resolve().parents[1]
WORKSPACE=ROOT.parent
SOURCES=[('rocket-hoenn','Rocket Hoenn / expansion 1.17.1','pokeemerald-expansion-expansion-1.17.1','EMERALD'),('heart-and-soul-2.0.6','Heart & Soul 2.0.6','Pokemon Heart and Soul/pokehns-expansion-Release-v2.0.6','POKEMON_HNS'),('soulgold-1.2.1','Soulgold 1.2.1','soulgold-1.2.1','EMERALD')]
HEADERS=['global.h','pokemon.h','pokemon_storage_system.h','save.h','constants/species.h','constants/moves.h','constants/items.h','constants/abilities.h','constants/pokeball.h']

def build(profile_id,label,source,game):
 repo=Path(source) if Path(source).is_absolute() else WORKSPACE/source
 work=ROOT/'.local/profile-build'/profile_id
 overlay=work/'include/constants';overlay.mkdir(parents=True,exist_ok=True)
 regions=json.loads((repo/'src/data/region_map/region_map_sections.json').read_text())
 regiontext='#ifndef GUARD_CONSTANTS_REGION_MAP_SECTIONS_H\n#define GUARD_CONSTANTS_REGION_MAP_SECTIONS_H\n#include "constants/global.h"\n'
 if game=='POKEMON_HNS':
  regiontext+='enum {\n'+''.join(f'{x["id"]} = 0,\n' for x in regions['map_sections'] if 'in_hns' not in x and x['id'] not in {m['id'] for m in regions['hns_map_sections']})+'\n'+''.join(x['id']+(' = 1' if i==0 else '')+',\n' for i,x in enumerate(regions['hns_map_sections']))+'MAPSEC_NONE, MAPSEC_COUNT };\n'
 else:regiontext+='enum {\n'+''.join(x['id']+',\n' for x in regions['map_sections'])+'MAPSEC_NONE, MAPSEC_COUNT };\n'
 maxid='0x7F' if profile_id.startswith('soulgold') else '0x'
 regiontext+='\n#define METLOC_SPECIAL_EGG '+('0x7FFD' if profile_id.startswith('soulgold') else '0xFD')+'\n#define METLOC_IN_GAME_TRADE '+('0x7FFE' if profile_id.startswith('soulgold') else '0xFE')+'\n#define METLOC_FATEFUL_ENCOUNTER '+('0x7FFF' if profile_id.startswith('soulgold') else '0xFF')+'\n#endif\n'
 (overlay/'region_map_sections.h').write_text(regiontext)
 if not (repo/'include/constants/map_groups.h').exists():
  groups=json.loads((repo/'data/maps/map_groups.json').read_text())
  lines=[]
  for gi,key in enumerate(groups['group_order']):
   for mi,name in enumerate(groups[key]):
    m=json.loads((repo/'data/maps'/name/'map.json').read_text())
    lines.append(f'#define {m["id"]} (({gi} << 8) | {mi})')
  (overlay/'map_groups.h').write_text('\n'.join(lines)+'\n')
 fields={}
 for kind in ['BoxPokemon','Pokemon','SaveBlock1','SaveBlock2','SaveBlock3','PokemonStorage']:
  fields['sizeof.'+kind]='sizeof(struct '+kind+')'
 members={'SaveBlock1':['playerPartyCount','playerParty','money','flags','vars','bag'], 'SaveBlock2':['playerName','playerGender','playerTrainerId','encryptionKey'], 'PokemonStorage':['currentBox','boxes','boxNames','boxWallpapers','fusions'], 'Pokemon':['status','level','hp','maxHP','attack','defense','speed','spAttack','spDefense'], 'BoxPokemon':['personality','otId','nickname','otName','secure'], 'SpeciesInfo':['baseHP','baseAttack','baseDefense','baseSpeed','baseSpAttack','baseSpDefense','types','catchRate','genderRatio','growthRate','abilities']}
 if game=='POKEMON_HNS':members['SaveBlock1']+=['saveVersion','saveVersionMagic']
 if profile_id.startswith('soulgold'):members['SaveBlock1']+=['pokemonStorageExtensionTail']
 else:members['BoxPokemon']+=['checksum']
 for kind,names in members.items():
  for name in names:fields[kind+'.'+name]=f'offsetof(struct {kind}, {name})'
 for name in ['SECTOR_DATA_SIZE','SECTOR_SIZE','SECTOR_SIGNATURE','NUM_SECTORS_PER_SLOT','SAVE_BLOCK_3_CHUNK_SIZE','TOTAL_BOXES_COUNT','IN_BOX_COUNT','BOX_NAME_LENGTH','SHINY_ODDS','POKEMON_NAME_LENGTH']:
  fields[name]=name
 if game=='POKEMON_HNS':fields.update(SAVE_VERSION='SAVE_VERSION',SAVE_VERSION_MAGIC='SAVE_VERSION_MAGIC')
 if profile_id.startswith('soulgold'):fields['SECTOR_SIGNATURE_BX21']='SECTOR_SIGNATURE_BX21'
 headers='\n'.join('#include "'+x+'"' for x in HEADERS)+'\n#include "constants/region_map_sections.h"\n#include <stddef.h>\n'
 symbols={}
 for path in HEADERS[4:]+['constants/region_map_sections.h']:
  p=overlay/'region_map_sections.h' if path.endswith('region_map_sections.h') else repo/'include'/path
  txt=re.sub(r'/\*.*?\*/|//[^\n]*','',p.read_text(),flags=re.S)
  for name in re.findall(r'^\s*(?:#define\s+)?((?:SPECIES|MOVE|ITEM|BALL|ABILITY|MAPSEC)_[A-Z0-9_]+)(?:\s*=|\s*,|\s+[0-9])',txt,re.M):symbols[name]=name
 fields['sizeof.SpeciesInfo']='sizeof(struct SpeciesInfo)'
 fields['sizeof.Ability']='sizeof(enum Ability)'
 bag_body=re.search(r'struct Bag\s*\{(.*?)\n\};',(repo/'include/global.h').read_text(),re.S)[1]
 for field in re.findall(r'struct ItemSlot\s+(\w+)\[',bag_body):
  fields['Bag.'+field]=f'offsetof(struct Bag, {field})'
  fields['Bag.count.'+field]=f'sizeof(((struct Bag *)0)->{field})/sizeof(struct ItemSlot)'
 fields.update(symbols)
 code=headers+'struct Entry { char name[80]; u32 value; };\nconst struct Entry fields[] = {\n'+''.join('{"'+k+'", '+v+'},\n' for k,v in fields.items())+'};\n'
 c=work/'layout.c';c.write_text(code)
 flags=['arm-none-eabi-gcc','-iquote',str(work/'include'),'-iquote',str(work/'include/constants'),'-iquote',str(repo/'include'),'-iquote',str(repo/'tools/agbcc/include'),'-iquote',str(repo),'-mabi=apcs-gnu','-mthumb','-mcpu=arm7tdmi','-DMODERN=1','-D'+game+'=1']
 result=subprocess.run(flags+['-c',str(c),'-o',str(work/'layout.o')],capture_output=True,text=True)
 if result.returncode:raise RuntimeError(result.stderr[:10000])
 subprocess.run(['arm-none-eabi-objcopy','-O','binary','--only-section=.rodata',str(work/'layout.o'),str(work/'layout.bin')],check=True)
 data=(work/'layout.bin').read_bytes();assert len(data)%84==0
 values={name.split(b'\0')[0].decode():value for name,value in struct.iter_unpack('<80sI',data)}
 section_list=regions['hns_map_sections'] if game=='POKEMON_HNS' else regions['map_sections']
 locations={str(values[x['id']]):x['name'] for x in section_list if x['id'] in values and 'name' in x}
 sizes=[values['sizeof.SaveBlock2']]+[min(3968,max(0,values['sizeof.SaveBlock1']-i*3968)) for i in range(4)]+[min(3968,max(0,values['sizeof.PokemonStorage']-i*3968)) for i in range(9)]
 profile={'id':profile_id,'name':label,'region':'johto-kanto' if game=='POKEMON_HNS' or profile_id.startswith('soulgold') else 'hoenn','source':repo.name,'gameDefine':game,'layout':{k:v for k,v in values.items() if not re.match(r'^(SPECIES|MOVE|ITEM|ABILITY|BALL|MAPSEC)_',k)},'checksumLengths':sizes,'locations':locations,'symbols':{prefix:{k:v for k,v in values.items() if k.startswith(prefix+'_')} for prefix in ['SPECIES','MOVE','ITEM','ABILITY','BALL']},'sourceHashes':{f:hashlib.sha256((repo/'include'/f).read_bytes()).hexdigest() for f in HEADERS},'status':'requires-gameplay-validation' if profile_id!='rocket-hoenn' else 'native-save-available'}
 profile['locationRegions']={}
 start=next((i for i,s in enumerate(section_list) if s['id']=='MAPSEC_PALLET_TOWN'),None)
 if start is not None:
  end=next((i for i,s in enumerate(section_list) if i>start and s['id'] in ['MAPSEC_SPECIAL_AREA','MAPSEC_NEW_BARK_TOWN']),len(section_list))
  for section in section_list[start:end]:profile['locationRegions'][str(values[section['id']])]='kanto'
 for section in section_list:
  if section['id']=='MAPSEC_ROCKET_HIDEOUT_HNS':profile['locationRegions'][str(values[section['id']])]='johto'
  if game=='POKEMON_HNS' and section['id'] in ['MAPSEC_VICTORY_ROAD','MAPSEC_SAFARI_ZONE']:profile['locationRegions'][str(values[section['id']])]='hoenn' if section['id']=='MAPSEC_VICTORY_ROAD' else 'johto'
 text=re.sub(r'/\*.*?\*/|//[^\n]*','',(repo/'include/pokemon.h').read_text(),flags=re.S)
 def members_of(body):return re.findall(r'(?:u(?:8|16|32)|enum \w+)\s+(\w+)\s*(?::\s*\d+)?\s*;',body)
 outer=re.search(r'struct BoxPokemon\s*\{(.*?)\n\};',text,re.S)[1].split('    union')[0].split('struct PokemonSecureData secure')[0]
 masks={('outer.'+n):n for n in members_of(outer) if n not in ['personality','otId','checksum']}
 if profile_id.startswith('soulgold'):
  body=re.search(r'struct PokemonSecureData\s*\{(.*?)\n\};',text,re.S)[1]
  masks.update({n:'secure.'+n for n in members_of(body)})
 else:
  for i in range(4):
   body=re.search(r'struct PokemonSubstruct'+str(i)+r'\s*\{(.*?)\n\};',text,re.S)[1]
   masks.update({n:f'secure.substructs[{i}].type{i}.'+n for n in members_of(body) if not n.startswith('unused_')})
 maskcode=headers+'struct Probe {char name[80];struct BoxPokemon mon;}; const struct Probe probes[] = {\n'+''.join('{"'+name+'", {.'+field+' = ~0u}},\n' for name,field in masks.items())+'};\n'
 (work/'masks.c').write_text(maskcode)
 result=subprocess.run(flags+['-w','-c',str(work/'masks.c'),'-o',str(work/'masks.o')],capture_output=True,text=True)
 if result.returncode:raise RuntimeError(result.stderr[:10000])
 subprocess.run(['arm-none-eabi-objcopy','-O','binary','--only-section=.rodata',str(work/'masks.o'),str(work/'masks.bin')],check=True)
 masked=(work/'masks.bin').read_bytes();stride=80+values['sizeof.BoxPokemon'];assert len(masked)%stride==0
 profile['bitFields']={}
 for i in range(0,len(masked),stride):
  name=masked[i:i+80].split(b'\0')[0].decode();mask=masked[i+80:i+stride]
  profile['bitFields'][name]=[b*8+bit for b,v in enumerate(mask) for bit in range(8) if v&(1<<bit)]
 if profile_id=='rocket-hoenn':
  elf=repo/'pokeemerald.elf';rom=repo/'pokeemerald.gba'
  symbol=re.search(r'^([0-9a-fA-F]+) [A-Za-z] gSpeciesInfo$',subprocess.check_output(['arm-none-eabi-nm',str(elf)],text=True),re.M)
  if symbol:
   raw=rom.read_bytes();start=int(symbol[1],16)-0x8000000;profile['personal']={}
   for index in set(profile['symbols']['SPECIES'].values()):
    if index>=2048:continue
    info=raw[start+index*values['sizeof.SpeciesInfo']:]
    fields_read={k:info[values['SpeciesInfo.'+k]] for k in ['baseHP','baseAttack','baseDefense','baseSpeed','baseSpAttack','baseSpDefense','catchRate','genderRatio','growthRate']}
    fields_read['types']=list(info[values['SpeciesInfo.types']:][:2])
    abilitypos=values['SpeciesInfo.abilities'];aw=values['sizeof.Ability'];fields_read['abilities']=[int.from_bytes(info[abilitypos+i*aw:abilitypos+(i+1)*aw],'little') for i in range(3)]
    profile['personal'][str(index)]=fields_read
 path=ROOT/'profiles'/f'{profile_id}.json';path.write_text(json.dumps(profile,indent=2,ensure_ascii=False)+'\n')
 print(profile_id,'party',values['sizeof.Pokemon'],'box',values['sizeof.BoxPokemon'],'storage',values['sizeof.PokemonStorage'],'boxes',values['TOTAL_BOXES_COUNT'],'checksums',sizes,flush=True)

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--profile');parser.add_argument('--repo',type=Path);parser.add_argument('--id');parser.add_argument('--name');parser.add_argument('--game-define',default='EMERALD');args=parser.parse_args()
 if args.repo:
  if not args.id:parser.error('--repo needs a unique --id')
  build(args.id,args.name or args.repo.name,str(args.repo.resolve()),args.game_define)
  raise SystemExit(0)
 for entry in SOURCES:
  if args.profile is None or args.profile==entry[0]:build(*entry)
