# -*- coding: utf-8 -*-
"""
Собирает Data/items.json (предметы DLC для каталогов приложения) из скриптов Rulebook/items/dlcNN.py.
Нужна собранная библиотека: py Rulebook/build_library.py

Запуск:  py Rulebook/build_items.py
"""
import glob
import importlib.util
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, 'items'))
from common import Out, ROOT  # noqa: E402


def main():
    out = Out()
    out.pack('core', 'Корник', 'Core Rulebook', 'core')
    for path in sorted(glob.glob(os.path.join(HERE, 'items', 'dlc*.py')) + glob.glob(os.path.join(HERE, 'items', 'bc*.py'))):
        name = os.path.splitext(os.path.basename(path))[0]
        spec = importlib.util.spec_from_file_location(name, path)
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)
        before = len(out.items)
        mod.build(out)
        print('%-8s +%d' % (name, len(out.items) - before))
    os.makedirs(os.path.join(ROOT, 'Data'), exist_ok=True)
    path = os.path.join(ROOT, 'Data', 'items.json')
    with open(path, 'w', encoding='utf-8') as f:
        json.dump({'version': 1, 'packs': out.packs, 'items': out.items}, f, ensure_ascii=False, indent=0, separators=(',', ':'))
    with open(os.path.join(ROOT, 'Data', 'npcs.json'), 'w', encoding='utf-8') as f:
        json.dump({'version': 1, 'npcs': out.npcs}, f, ensure_ascii=False, indent=0, separators=(',', ':'))
    print('НИПов: %d' % len(out.npcs))
    print('всего %d предметов, %d пакетов -> %s (%.0f КБ)' % (len(out.items), len(out.packs), path, os.path.getsize(path) / 1024))


if __name__ == '__main__':
    main()
