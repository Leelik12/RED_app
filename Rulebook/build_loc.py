"""Собирает Data/loc_en.json из Rulebook/loc_en.txt (строки «русский => английский»).

Использование: python Rulebook/build_loc.py [--coverage harvest.txt]
--coverage печатает строки сбора, которых нет в словаре (точное совпадение или шаблон).
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'loc_en.txt')
DST = os.path.join(HERE, '..', 'Data', 'loc_en.json')
SEP = ' => '


def load():
    out = {}
    for n, line in enumerate(open(SRC, encoding='utf-8'), 1):
        line = line.rstrip('\r\n')
        if not line.strip() or line.lstrip().startswith('#'):
            continue
        if SEP not in line:
            print(f'строка {n}: нет разделителя: {line}')
            continue
        ru, en = line.split(SEP, 1)
        ru, en = ru.strip(), en.strip()
        if not ru or not en:
            print(f'строка {n}: пусто: {line}')
            continue
        if ru in out and out[ru] != en:
            print(f'строка {n}: конфликт для «{ru}»: «{out[ru]}» и «{en}»')
        out[ru] = en
    # названия книг (DLC) берём из items.json: там официальные английские названия
    try:
        packs = json.load(open(os.path.join(HERE, '..', 'Data', 'items.json'), encoding='utf-8'))['packs']
        for p in packs:
            ru, en = (p.get('name') or '').strip(), (p.get('nameEn') or '').strip()
            if ru and en and p.get('kind') != 'core':
                if ru in out and out[ru] != en:
                    print(f'книга «{ru}»: словарь «{out[ru]}» заменён на «{en}»')
                out[ru] = en
    except Exception as ex:
        print('items.json:', ex)
    return out


def pattern(ru):
    parts = re.split(r'(\{[#*]\})', ru)
    rx = ''
    for p in parts:
        if p == '{#}':
            rx += r'(\d+(?:[.,]\d+)?)'
        elif p == '{*}':
            rx += r'(.+?)'
        else:
            rx += re.sub(r'(?:\\ )+', lambda m: r'\s+', re.escape(p))
    return re.compile('^' + rx + '$')


def covered(s, exact, pats):
    t = s.strip()
    if t in exact:
        return True
    return any(p.match(t) for p in pats)


def main():
    data = load()
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, 'w', encoding='utf-8') as f:
        json.dump(dict(sorted(data.items())), f, ensure_ascii=False, indent=0)
    print(f'{len(data)} записей -> {os.path.normpath(DST)}')

    if '--coverage' in sys.argv:
        path = sys.argv[sys.argv.index('--coverage') + 1]
        exact = {k for k in data if '{' not in k}
        pats = [pattern(k) for k in data if '{#}' in k or '{*}' in k]
        miss = [l.rstrip('\r\n') for l in open(path, encoding='utf-8') if l.strip() and not covered(l.rstrip('\r\n'), exact, pats)]
        out = os.path.join(os.path.dirname(path), 'loc_missing.txt')
        open(out, 'w', encoding='utf-8').write('\n'.join(miss))
        print(f'не покрыто: {len(miss)} -> {out}')


if __name__ == '__main__':
    main()
