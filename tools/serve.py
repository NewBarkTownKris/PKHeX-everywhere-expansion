#!/usr/bin/env python3
from pathlib import Path
from http.server import SimpleHTTPRequestHandler,ThreadingHTTPServer
import argparse
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--port',type=int,default=8765);p.add_argument('--directory',type=Path,default=root/'src/PKHeX.Web.React/dist');args=p.parse_args()
folder=args.directory.resolve()
if not (folder/'index.html').exists():raise SystemExit('Build the site with tools/build-web.sh first.')
class Handler(SimpleHTTPRequestHandler):
 def __init__(self,*a,**kw):super().__init__(*a,directory=str(folder),**kw)
 def do_GET(self):
  path=Path(self.translate_path(self.path))
  if not path.exists() and not Path(self.path.split('?',1)[0]).suffix and not self.path.startswith('/_framework/'):self.path='/index.html'
  super().do_GET()
print(f'PKHeX Expansion Web: http://127.0.0.1:{args.port}',flush=True)
ThreadingHTTPServer(('127.0.0.1',args.port),Handler).serve_forever()
