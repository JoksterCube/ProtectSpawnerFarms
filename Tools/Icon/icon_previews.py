"""Renders the mod's painted icons (status and inventory buttons) into Docs/Icons/<Name>.png, for the README.

The icons are drawn by the mod's own code (StatusIcons, ButtonIcons), so this:
1. runs the IconPreview test's Render_icons, which writes each icon as .rgba to a temporary folder;
2. turns its output into PNGs.

Needs only Python and a Release build of the mod (the tests use it).
Usage: python icon_previews.py
"""
import os
import struct
import subprocess
import tempfile
import zlib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Docs', 'Icons')


def write_png(path, width, height, rgba):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)

    rows = b''.join(b'\x00' + rgba[y * width * 4:(y + 1) * width * 4] for y in range(height))
    with open(path, 'wb') as f:
        f.write(b'\x89PNG\r\n\x1a\n')
        f.write(chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)))
        f.write(chunk(b'IDAT', zlib.compress(rows, 9)))
        f.write(chunk(b'IEND', b''))


def main():
    os.makedirs(OUT, exist_ok=True)
    with tempfile.TemporaryDirectory() as rendered:
        env = dict(os.environ, PROTECTSPAWNERFARMS_ICON_OUT=rendered)
        subprocess.run(['dotnet', 'test', os.path.join(ROOT, 'Tests', 'ProtectSpawnerFarms.Tests'), '--filter', 'Render_icons'],
                       check=True, env=env)
        for file in sorted(os.listdir(rendered)):
            with open(os.path.join(rendered, file), 'rb') as f:
                width, height = struct.unpack('<ii', f.read(8))
                write_png(os.path.join(OUT, file[:-5] + '.png'), width, height, f.read(width * height * 4))
            print('wrote', file[:-5] + '.png')


if __name__ == '__main__':
    main()
