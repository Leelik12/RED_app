# -*- coding: utf-8 -*-
"""Извлечение блоков НИПов (красные плашки R. Talsorian) из PDF DLC по координатам текстового слоя.
Результат — черновики словарей; финальные данные правятся вручную в dlcNN.py и сверяются по картинкам страниц."""
import os
import re
import sys

import fitz

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CYR = re.compile('[А-Яа-яЁё]')
DMG = re.compile(r'(\d+d\d+(?:\s*[+/]\s*\d+)?)')
STATS = ['INT', 'REF', 'DEX', 'TECH', 'COOL', 'WILL', 'LUCK', 'MOVE', 'BODY', 'EMP']


def pdf_path(name):
    return os.path.join(ROOT, 'DLC', name)


def _clean(t):
    return re.sub(r'\s+', ' ', t.replace('\n', ' ')).strip()


def _tokens(words, y0, y1, x0=130, x1=520):
    """слова-числа в области, сгруппированные по двум строкам; «(6)» приклеивается к предыдущему."""
    ws = [w for w in words if y0 - 2 <= w[1] <= y1 + 2 and x0 <= w[0] <= x1]
    ws.sort(key=lambda w: (round(w[1] / 14), w[0]))
    return ws


def extract_page(page):
    blocks = [(b[0], b[1], b[2], b[3], _clean(b[4])) for b in page.get_text('blocks')]
    words = page.get_text('words')
    # якоря: блок из 10 значений статов (в тексте это строки из одиночных чисел/тире)
    anchors = []
    for b in page.get_text('blocks'):
        lines = [l.strip() for l in b[4].split('\n') if l.strip()]
        if len(lines) >= 9 and all(re.fullmatch(r'[\d—\-–]+|\d+ \(\d+\)', l) for l in lines):
            anchors.append(b)
    anchors.sort(key=lambda b: b[1])
    out = []
    for i, a in enumerate(anchors):
        top = a[1] - 20
        bottom = anchors[i + 1][1] - 20 if i + 1 < len(anchors) else 10 ** 6
        inside = [b for b in blocks if top <= b[1] < bottom]
        rec = {'page': page.number + 1}
        # статы: слова в области блока
        ws = [w for w in words if a[1] - 3 <= w[1] <= a[3] + 3 and a[0] - 3 <= w[0] <= a[2] + 3]
        ws.sort(key=lambda w: (round(w[1] / 12), w[0]))
        vals = []
        for w in ws:
            t = w[4]
            if re.fullmatch(r'\(\d+\)', t) and vals:
                vals[-1] = vals[-1] + ' ' + t
            elif re.fullmatch(r'[\d—\-–]+', t):
                vals.append(t)
        # «6 (4)» может лежать отдельным блоком рядом со статами
        extra = [b for b in blocks if re.fullmatch(r'\d+ \(\d+\)', b[4]) and a[1] - 30 <= b[1] <= a[3] + 30]
        rec['stat_raw'] = vals
        rec['stat_extra'] = [(b[0], b[4]) for b in extra]
        # ПЗ / тяжёлое ранение / спасбросок
        hp = [b for b in inside if re.fullmatch(r'\d+ \d+ \d+', b[4])]
        if hp:
            rec['hp'] = [int(x) for x in hp[0][4].split()]
        # название — вертикальный блок слева
        title = [b for b in inside if b[0] < 112 and b[2] < 125 and CYR.search(b[4]) and b[3] - b[1] > 60]
        rec['title'] = title[0][4] if title else ''
        # защита
        armor = [b for b in inside if b[4].startswith('Защита')]
        rec['armor_type'] = armor[0][4].replace('Защита:', '').strip() if armor else ''
        os_ = sorted([b for b in inside if re.fullmatch(r'\d+ ОС', b[4])], key=lambda b: b[1])
        rec['os'] = [int(b[4].split()[0]) for b in os_]
        # навыки и снаряжение
        long_ = [b for b in inside if len(b[4]) > 70 and b[0] > 120 and re.search(r'\d', b[4])]
        skills = [b for b in long_ if re.search(r'Уклонение|Атлетика|Концентрация|Общение|Внимательность', b[4]) and b[4].count(',') > 6]
        rec['skills'] = skills[0][4] if skills else ''
        rest = [b for b in inside if b[1] > (skills[0][3] if skills else a[3]) and b[0] > 150 and len(b[4]) > 25 and not b[4].startswith('Что ') and CYR.search(b[4])]
        gear = [b for b in rest if b is not (skills[0] if skills else None)]
        rec['gear'] = _clean(' '.join(b[4] for b in gear[:1]))
        # ролевая способность
        role = [b for b in inside if re.match(r'^(Интерфейс|Боевое чутье|Медицина|Создатель|Командная|Харизматическое|Доверие|Операция|Мощь)', b[4])]
        rec['role'] = role[0][4] if role else ''
        # оружие: кириллические блоки слева между «Оружие» и навыками; пары «имя … кубы»
        wy0 = a[3] + 40
        wy1 = (skills[0][1] if skills else a[3] + 120) - 4
        wb = [b for b in inside if wy0 <= b[1] < wy1 and b[2] < 405 and CYR.search(b[4]) and b[4] not in ('Оружие', 'Weapons') and 'Защита' not in b[4]]
        wb.sort(key=lambda b: (b[1], b[0]))
        text = ' '.join(b[4] for b in wb)
        weapons = []
        pos = 0
        for m in DMG.finditer(text):
            name = text[pos:m.start()].strip(' ,;')
            if name:
                weapons.append((name, m.group(1)))
            pos = m.end()
        rec['weapons'] = weapons
        out.append(rec)
    return out


def extract(pdf, pages):
    doc = fitz.open(pdf_path(pdf))
    res = []
    for p in pages:
        res.extend(extract_page(doc[p - 1]))
    return res


if __name__ == '__main__':
    import json
    name = sys.argv[1]
    pages = [int(x) for x in sys.argv[2:]]
    for r in extract(name, pages):
        print(json.dumps(r, ensure_ascii=False))


WEAPON_FIX = {
    'оч. тяжелый пистолет': 'Очень тяжёлый пистолет', 'оч. тяжёлый пистолет': 'Очень тяжёлый пистолет',
    'тяж. ор. ближ. боя': 'Тяжёлое оружие ближнего боя', 'штуромовая винтовка': 'Штурмовая винтовка',
    'тяжелый пп': 'Тяжёлый ПП', 'тяжёлый пп': 'Тяжёлый ПП', 'тяжелый пистолет': 'Тяжёлый пистолет',
    'ср. пистолет': 'Средний пистолет', 'лёгкий ор. ближ. боя': 'Лёгкое оружие ближнего боя',
    'легкий ор. ближ. боя': 'Лёгкое оружие ближнего боя', 'оч. тяж. ор. ближ. боя': 'Очень тяжёлое оружие ближнего боя',
    'ср. ор. ближ. боя': 'Среднее оружие ближнего боя', 'гранатомет': 'Гранатомёт', 'гранатомёт': 'Гранатомёт',
}
CYBER_WORDS = ('Нейролинк', 'Киберрук', 'Киберног', 'Киберглаз', 'Керезников', 'Личный порт', 'Искусственные мышцы', 'Эндоскелет',
               'Подкожная броня', 'Тканекожа', 'Химкожа', 'Биомонитор', 'Контрацептив', 'Усиленные', 'Разъем', 'Разъём')


def pretty(title, extra=None):
    t = title.replace('ПРОЖЖЕНЫЙ', 'ПРОЖЖЁННЫЙ').replace('ПРОЖЖЕННЫЙ', 'ПРОЖЖЁННЫЙ').replace('ПРОЖЖЕНЫЕ', 'ПРОЖЖЁННЫЕ')
    t = t.lower().capitalize()
    for a, b in (('(против', '(против'),):
        t = t.replace(a, b)
    if extra:
        for k, v in extra.items():
            t = t.replace(k, v)
    return t


def weapon_name(n):
    n = re.sub(r'\s*Плох\. ?кач-ва', ' плохого качества', n)
    n = re.sub(r'\s*Отл\. ?кач-ва', ' отличного качества', n)
    n = n.replace('О.Т. Пистолет', 'Очень тяжёлый пистолет').replace('Штурм. Винтовка', 'Штурмовая винтовка')
    k = n.strip().lower()
    return WEAPON_FIX.get(k, n.strip()[:1].upper() + n.strip()[1:].lower() if n.isupper() or n.istitle() else n.strip())


def build_npcs(out, pack, group, pdf, pages, tier='', fixes=None, repl=None, info=None, only=None):
    """Добавляет НИПов из PDF в out.npcs. fixes[title_prefix] = dict(...) — ручные правки полей npc()."""
    from common import npc
    fixes = fixes or {}
    recs = extract(pdf, pages)
    for r in recs:
        title = r['title']
        if only and not any(o in title for o in only):
            continue
        raw = r['stat_raw']
        if len(raw) < 10:
            print('! мало статов', title, raw)
            continue
        vals, base = [], []
        for t in raw:
            m = re.fullmatch(r'(\d+) \((\d+)\)', t)
            if m:
                base.append(int(m.group(1))); vals.append(int(m.group(2)))
            else:
                n = int(t) if t.isdigit() else 0
                base.append(n); vals.append(n)
        paren = base != vals
        weapons = '\n'.join('%s %s' % (weapon_name(n), d) for n, d in r['weapons'])
        gear_items = [g.strip() for g in re.split(r',\s*(?![^()]*\))|•', r['gear']) if g.strip()]
        cyber = [g for g in gear_items if any(w in g for w in CYBER_WORDS)]
        gear = [g for g in gear_items if g not in cyber]
        skills = re.sub(r'Бизнес (?=Скрытие)', '', r['skills']).replace('/ ', '/')
        armor = r['armor_type']
        name = pretty(title, repl)
        note = ''
        if paren:
            note = 'Значения в скобках из книги (с учётом штрафа брони) использованы как основные; без штрафа: ' + \
                   ', '.join('%s %d' % (k, b) for k, b, v in zip(['ИНТ', 'РЕА', 'ЛВК', 'ТЕХ', 'ХАР', 'ВОЛЯ', 'УДЧ', 'СКО', 'ТЕЛ', 'ЭМП'], base, vals) if b != v) + '.'
        kw = dict(stats=vals, hp=r['hp'][0], wound=r['hp'][1], ds=r['hp'][2],
                  head=r['os'][0] if r['os'] else 0, body=r['os'][1] if len(r['os']) > 1 else (r['os'][0] if r['os'] else 0),
                  weapons=weapons, skills=skills, gear=', '.join(gear), cyber=', '.join(cyber), role=r['role'], tier=tier,
                  info=' '.join(x for x in [('Броня: ' + armor + '.') if armor else '', note, (info or '')] if x), page=r['page'], paren=paren)
        for pre, fx in fixes.items():
            if title.startswith(pre) or name.startswith(pre):
                kw.update(fx)
        npc(out, pack, group, name, **kw)
