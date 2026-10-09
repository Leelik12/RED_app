# -*- coding: utf-8 -*-
"""Карта Найт-Сити: районы A–X из корника (RU, гл. «Добро пожаловать в Найт-Сити») -> Data/map.json.
Положение районов (x, y) — схематичное, снято с карты на с. 310 английского издания; районов без точного места
на карте (Даунтаун, Зона восстановителей, Порт) расставлено по соседям и помечено approx."""
import json, os, re, sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'items'))

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'ru', '14_ДОБРО_ПОЖАЛОВАТЬ_В_НАЙТ-СИТИ.md')
OUT = os.path.join(HERE, '..', 'Data', 'map.json')

# letter: (x, y, zone, approx)
POS = {
    'A': (257, 445, 'rebuilding', False), 'B': (490, 408, 'rebuilding', False), 'C': (300, 395, 'rebuilding', True),
    'D': (390, 455, 'hot', False), 'E': (535, 520, 'combat', False), 'F': (258, 515, 'rebuilding', False),
    'G': (355, 600, 'rebuilding', False), 'H': (450, 540, 'combat', False), 'I': (258, 610, 'combat', False),
    'J': (310, 700, 'combat', True), 'K': (610, 470, 'perimeter', True), 'L': (450, 650, 'combat', False),
    'M': (357, 135, 'rebuilding', False), 'N': (390, 225, 'rebuilding', False), 'O': (400, 300, 'rebuilding', True),
    'P': (575, 278, 'suburbs', False), 'Q': (505, 185, 'suburbs', True), 'R': (665, 385, 'executive', False),
    'S': (590, 570, 'suburbs', True), 'T': (650, 650, 'suburbs', True), 'U': (575, 690, 'suburbs', False),
    'V': (640, 770, 'suburbs', False), 'W': (255, 830, 'rebuilding', False), 'X': (415, 865, 'suburbs', False),
}
# положение на растровой карте с. 312–313 RU (4250x2750, Content/night_city_map.jpg); J, R, C — по соседям
# координаты сняты с горизонтального разворота 4250x2750; в игре картинка повёрнута на 90° по часовой, чтобы текст читался
MPOS_RAW = {
    'A': (1830, 1730), 'B': (1770, 1280), 'C': (1900, 1950), 'D': (1940, 1460), 'E': (2000, 1110), 'F': (2080, 1720),
    'G': (2240, 1550), 'H': (2150, 1260), 'I': (2450, 1600), 'J': (1000, 1760), 'K': (2435, 1722), 'L': (2330, 1300),
    'M': (1130, 1600), 'N': (1250, 1470), 'O': (1400, 1388), 'P': (1700, 930), 'Q': (1630, 780), 'R': (2960, 640),
    'S': (2320, 947), 'T': (2230, 700), 'U': (2510, 1040), 'V': (2700, 880), 'W': (2780, 1680), 'X': (2900, 1500),
}
MPOS = {k: (2750 - y, x) for k, (x, y) in MPOS_RAW.items()}
SIDE = {'A': 'Остров', 'B': 'Остров', 'C': 'Остров', 'D': 'Остров', 'E': 'Остров', 'F': 'Остров', 'G': 'Остров', 'H': 'Остров',
        'I': 'Остров', 'J': 'Остров', 'K': 'Остров', 'L': 'Остров', 'M': 'Северная сторона', 'N': 'Северная сторона',
        'O': 'Северная сторона', 'P': 'Материк', 'Q': 'Материк', 'R': 'Материк', 'S': 'Материк', 'T': 'Материк', 'U': 'Материк',
        'V': 'Материк', 'W': 'Южная сторона', 'X': 'Южная сторона'}

# разрывы слов от переноса строк в исходнике: вручную то, что не поймала проверка по словарю корпуса
FIX = [('поли гон', 'полигон'), ('вос становленными', 'восстановленными'), ('зда ние', 'здание'), ('ста дион', 'стадион'), ('попу лярный', 'популярный'),
       ('сту дийные', 'студийные'), ('инстал ляция', 'инсталляция'), ('туристиче ская', 'туристическая'), ('архитек турная', 'архитектурная'),
       ('завсег датаи', 'завсегдатаи'), ('сго воре', 'сговоре'), ('Суз уки', 'Сузуки'), ('Контей нерное', 'Контейнерное'), ('Малень кого', 'Маленького'),
       ('Обыч ный', 'Обычный'), ('ресто ран', 'ресторан'), ('Квар тал', 'Квартал'), ('квар тира', 'квартира'), ('Гео дезический', 'Геодезический'),
       ('гео дезический', 'геодезический'), ('Комью нити', 'Комьюнити'), ('сам пар тизанский', 'сам партизанский'), ('ориенти рованный', 'ориентированный'), ('мульти культурный', 'мультикультурный'), ('\u0301', '')]


def _freq():
    f = {}
    folder = os.path.join(HERE, 'ru')
    for name in os.listdir(folder):
        if name.endswith('.md'):
            for w in re.findall(r'[а-яё]+', open(os.path.join(folder, name), encoding='utf-8').read().lower()):
                f[w] = f.get(w, 0) + 1
    return f


FREQ = _freq()


def _rejoin(t):
    """склеивает «покры тый» в «покрытый», если целое слово есть в корпусе, а обломок редок"""
    toks = t.split(' ')
    out, i = [], 0
    while i < len(toks):
        a = toks[i]
        if i + 1 < len(toks):
            ma = re.fullmatch(r'([^а-яё]*)([а-яё]{2,})', a)
            mb = re.fullmatch(r'([а-яё]{2,})([^а-яё]*)', toks[i + 1])
            if ma and mb:
                w = ma.group(2) + mb.group(1)
                if FREQ.get(w, 0) >= 1 and min(FREQ.get(ma.group(2), 0), FREQ.get(mb.group(1), 0)) <= 2:
                    out.append(ma.group(1) + w + mb.group(2))
                    i += 2
                    continue
        out.append(a)
        i += 1
    return ' '.join(out)


def _stars(t):
    """«кон** тейнеров» — разрыв слова с остатком жирной разметки; «грани** Найт-Сити» — просто пробел"""
    def rep(m):
        a, b = m.group(1), m.group(2)
        return a + b if FREQ.get((a + b).lower(), 0) >= 1 else a + ' ' + b
    t = re.sub(r'([а-яёА-ЯЁ]+)\*\*\s*([а-яё]+)', rep, t)
    return re.sub(r'\s*\*\*\s*', ' ', t)


LAT = {'А': 'A', 'В': 'B', 'С': 'C', 'Е': 'E', 'Н': 'H', 'К': 'K', 'М': 'M', 'О': 'O', 'Р': 'P', 'Т': 'T', 'Х': 'X'}
LET = 'A-XАВСЕНКМОРТХ'


def lat(c):
    return LAT.get(c, c)


def clean(t):
    t = _stars(re.sub(r'\s+', ' ', t))
    # перенос строки в исходнике оставил «Найт- Сити»; настоящие дефисы в составных словах сохраняем
    t = re.sub(r'(?<=[А-Яа-яЁёA-Za-z])-\s+(?=[А-ЯЁA-Za-zа-яё])', '-', t)
    t = _rejoin(t)
    for a, b in FIX:
        t = t.replace(a, b)
    # хвост с заголовком следующего района («... Доки Хейвуда (S)»)
    t = re.sub(r'\s+[А-ЯЁ][А-Яа-яЁё ]+\([' + LET + r']\)$', '', t)
    return t.strip(' *▶◀')


def main():
    lines = open(SRC, encoding='utf-8').read().split('\n')
    start = next(i for i, l in enumerate(lines) if re.match(r'^[A-XАВСЕНКМОРТХ]\s*\.{2,}', l) and lat(l[0]) == 'A')
    legend = {}
    i = start
    while i < len(lines) and len(legend) < 24:
        m = re.match(r'^([' + LET + r'])\s*\.{2,}\s*(.+)$', lines[i])
        if m:
            legend[lat(m.group(1))] = m.group(2).strip()
        i += 1
    assert len(legend) == 24, legend

    # тело: с первого «### СТРОВ» до конца, без разметки страниц, врезок и подписей
    body_start = next(i for i, l in enumerate(lines) if l.startswith('### СТРОВ'))
    chunk = []
    for l in lines[body_start:]:
        if l.startswith('---') or l.startswith('[стр.') or l.startswith('>') or l.strip() in ('ИНФО',) or l.startswith('НИЛ ') or l.startswith('BY '):
            continue
        chunk.append(l)
    text = '\n'.join(chunk)

    # сити-менеджеры по порядку A..X
    mgr = list(re.finditer(r'Сити-менеджер:\s*(.*?)\s*Обеспечение безопасности:\s*(.*?)\s*Присутствующие банды:\s*(.*?)(?=\n\n|\n#|\n\*\*|\n\()', text, re.S))
    letters = sorted(legend)
    assert len(mgr) == 24, len(mgr)
    districts = {}
    for L, m in zip(letters, mgr):
        districts[L] = {'letter': L, 'name': legend[L].replace('Горячая Зона', 'Горячая зона'), 'side': SIDE[L], 'x': POS[L][0], 'y': POS[L][1], 'mx': MPOS[L][0], 'my': MPOS[L][1], 'kind': POS[L][2], 'approx': POS[L][3],
                        'manager': clean(m.group(1)), 'security': clean(m.group(2)), 'gangs': clean(m.group(3)), 'places': []}

    flat = re.sub(r'\n+', ' ', text)
    pat = re.compile(r'\(([' + LET + r'])(\d+)\)\s*([^:()]{2,70}?):\s*(.*?)(?=\s*\([' + LET + r']\d+\)|\s*#{2,}|\s*Сити-менеджер:|\s*\*\*▶|$)')
    for m in pat.finditer(flat):
        L, n, name, desc = lat(m.group(1)), int(m.group(2)), m.group(3), m.group(4)
        desc = re.split(r'\s(?:Сити-менеджер:|#{2,})', desc)[0]
        districts[L]['places'].append({'code': L + str(n), 'name': clean(name), 'text': clean(desc)})
    out = {'version': 1, 'source': 'Корник Cyberpunk RED v1.3 (RU), гл. «Добро пожаловать в Найт-Сити»',
           'note': 'Положение районов схематично, по карте на с. 310 английского издания.',
           'districts': [districts[L] for L in letters]}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    json.dump(out, open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('районов', len(letters), 'мест', sum(len(d['places']) for d in districts.values()))
    for L in letters:
        d = districts[L]
        print(L, d['name'], '|', d['manager'][:40], '|', len(d['places']))


if __name__ == '__main__':
    main()
