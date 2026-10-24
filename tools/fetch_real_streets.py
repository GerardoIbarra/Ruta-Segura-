import urllib.request
import json

query = """
[out:json][timeout:25];
(
  way["highway"](20.6740,-103.3510,20.6775,-103.3465);
);
out body;
>;
out skel qt;
"""

req = urllib.request.Request(
    'https://overpass-api.de/api/interpreter',
    data=query.encode('utf-8'),
    headers={'User-Agent': 'RutaSegura/1.0'}
)
resp = urllib.request.urlopen(req)
data = json.loads(resp.read().decode('utf-8'))
highways = set(
    e.get('tags', {}).get('name')
    for e in data.get('elements', [])
    if 'highway' in e.get('tags', {}) and 'name' in e.get('tags', {})
)
print("Calles encontradas en la zona real:")
for name in sorted(filter(None, highways)):
    print(f" - {name}")
