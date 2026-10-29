#!/usr/bin/env python3
from pathlib import Path
import io,json,shutil,tarfile,tempfile,urllib.request
root=Path(__file__).resolve().parents[1]
for item in json.loads((root/'UPSTREAM-SUBMODULES.json').read_text()):
 name=item['path'];target=root/'external'/name
 expected=target/('PKHeX.Core/PKHeX.Core.csproj' if name=='PKHeX' else 'PKHeX.Core.AutoMod/PKHeX.Core.AutoMod.csproj')
 if expected.exists():continue
 if target.exists() and any(target.iterdir()):raise SystemExit(f'{target} is incomplete; refusing to overwrite it.')
 (root/'.local').mkdir(exist_ok=True)
 url=f'https://codeload.github.com/arleypadua/{name}/tar.gz/{item["sha"]}'
 print('Downloading pinned dependency',name,item['sha'],flush=True)
 data=urllib.request.urlopen(url,timeout=120).read()
 with tempfile.TemporaryDirectory(dir=root/'.local') as work:
  with tarfile.open(fileobj=io.BytesIO(data),mode='r:gz') as archive:archive.extractall(work,filter='data')
  contents=list(Path(work).iterdir());assert len(contents)==1
  if target.exists():target.rmdir()
  shutil.move(contents[0],target)
