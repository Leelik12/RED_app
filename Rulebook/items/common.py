# -*- coding: utf-8 -*-
"""Общие помощники для скриптов сборки Data/items.json из библиотеки (Content/library_preview.json)."""
import json
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LIB_PATH = os.path.join(ROOT, 'Content', 'library_preview.json')

_lib = None


def lib():
    global _lib
    if _lib is None:
        _lib = json.load(open(LIB_PATH, encoding='utf-8'))
    return _lib


def source(sid):
    for s in lib()['sources']:
        if s['id'] == sid:
            return s
    raise KeyError(sid)


def entries(sid):
    out = []
    for c in source(sid)['chapters']:
        out.extend(c['entries'])
    return out


_LAT = dict(zip('ABCEHKMOPTXYaceopxy', 'АВСЕНКМОРТХУасеорху'))


def norm(t):
    """Латинские двойники букв -> кириллица, регистр не важен (в PDF они перемешаны)."""
    return ''.join(_LAT.get(c, c) for c in t).lower()


def find(sid, sub, kind=None, nth=0):
    """Запись по подстроке заголовка (без учёта регистра и латинских двойников)."""
    sub = norm(sub)
    hits = [e for e in entries(sid) if sub in norm(e['title']) and (kind is None or e['kind'] == kind)]
    if len(hits) <= nth:
        raise KeyError('%s: %r (найдено %d)' % (sid, sub, len(hits)))
    return hits[nth]


COST_RE = re.compile(r'(\d[\d,.  ]*)\s*(?:eb|еб|ЕБ|Eb)\b')


def parse_cost(text, after=None):
    t = text
    if after:
        i = t.find(after)
        if i >= 0:
            t = t[i:]
    m = COST_RE.search(t)
    if not m:
        return 0
    d = re.sub(r'[^\d]', '', m.group(1))
    return int(d) if d else 0


HL_RE = re.compile(r'(?:Потеря Человечности|ПЧ|HL)[: ]\s*(\d+\s*\(\d+d\d+\))')
INSTALL_RE = re.compile(r'Установка:\s*([А-Яа-яA-Za-zёЁ]+)')


def parse_hl(text):
    m = HL_RE.search(text)
    return re.sub(r'\s+', ' ', m.group(1)) if m else None


def parse_install(text):
    m = INSTALL_RE.search(text)
    return m.group(1) if m else None


MECH = re.compile(r'(Модуль|Опция|Боргирование|Нейронн|Парные|Внешний|Внутренний|ОС\s?\d|СЛ\s?\d|урон|Урон|слот|Слот|Действи|[+\-−]\d|\d+d\d|Одноруч|Двуруч|Экзотич|Требует|Щепка|Киберруки|Киберконечност)')


def body_info(text, limit=900):
    """Правила предмета: абзацы начиная с первого «механического», без цитат и цены."""
    paras = [p.strip() for p in text.split('\n\n') if p.strip() and not p.strip().startswith('>')]
    paras = [p for p in paras if not re.match(r'^Цена', p) or len(p) > 120]
    start = 0
    for i, p in enumerate(paras):
        if MECH.search(p):
            start = i
            break
    out = ' '.join(paras[start:])
    out = re.sub(r'\s+', ' ', out).strip()
    if len(out) > limit:
        cut = out.rfind('. ', 0, limit)
        out = out[:cut + 1 if cut > limit // 2 else limit].strip()
    return out


class Out:
    def __init__(self):
        self.packs = []
        self.items = []
        self.npcs = []

    def pack(self, pid, name, name_en=None, kind='dlc'):
        self.packs.append({'id': pid, 'name': name, 'nameEn': name_en or name, 'kind': kind})

    def add(self, pack, kind, name, **kw):
        it = {'pack': pack, 'kind': kind, 'name': name}
        for k, v in kw.items():
            if v in (None, '', 0, False):
                continue
            it[k] = v
        it['id'] = '%s.%03d' % (pack, sum(1 for x in self.items if x['pack'] == pack) + 1)
        self.items.append(it)
        return it


_NUM = re.compile(r'^(.*?)\s+(\d+)(?:\s*\((\d+)\))?$')
_CANON = None
_ALIAS = {
    'сопротивление пыткам/наркотикам': 'Сопр. пыткам/наркотикам',
    'автомат. огонь': 'Автоматический огонь (x2)',
    'оружие кр. калибра': 'Оружие кр. калибра (x2)',
    'оружие крупного калибра': 'Оружие кр. калибра (x2)',
    'обращение с животными': 'Обращ. с животными',
    'кино-/фототехника': 'Кино- и фототехника',
    'знание местности (ваш дом)': 'Знание местности (Твой дом)',
    'знание местности (выбор гм)': 'Знание местности',
}


def _canon():
    global _CANON
    if _CANON is None:
        src = open(os.path.join(ROOT, 'CoreDataBase.Skills.cs'), encoding='utf-8-sig').read()
        _CANON = {}
        for n in re.findall(r'Name = "([^"]+)"', src):
            _CANON[re.sub(r'\s*\(x2\)', '', n).lower()] = n
    return _CANON


def skill_name(n):
    k = re.sub(r'\s+', ' ', n.strip()).lower()
    if k in _ALIAS:
        return _ALIAS[k]
    return _canon().get(k, n.strip())


def parse_skills(text, paren=False):
    """«Атлетика 9, Скрытность 8 (6)» -> {'Атлетика': 9, 'Скрытность': 8}; значение в скобках — со штрафом брони (paren=True берёт его)."""
    out = {}
    for part in re.split(r',\s*(?![^()]*\))', re.sub(r'\s+', ' ', text.strip())):
        m = _NUM.match(part.strip())
        if m:
            v = int(m.group(3)) if paren and m.group(3) else int(m.group(2))
            out[skill_name(m.group(1))] = v
    return out


def npc(out, pack, group, name, stats, hp, wound, ds, head, body, weapons, skills, gear='', cyber='', role='', tier='', info='', page=0, move=None, paren=False):
    """stats = ИНТ, РЕА, ЛВК, ТЕХ, ХАР, ВОЛЯ, УДЧ, СКО, ТЕЛ, ЭМП (как в блоке книги); НИПы из книги всегда в режиме полного расчёта."""
    keys = ['INT', 'REF', 'DEX', 'TECH', 'COOL', 'WILL', 'LUCK', 'MOVE', 'BODY', 'EMP']
    rec = {
        'id': '%s.npc%02d' % (pack, sum(1 for n in out.npcs if n['pack'] == pack) + 1),
        'pack': pack, 'group': group, 'name': name, 'tier': tier, 'hp': hp, 'wound': wound, 'ds': ds,
        'head': head, 'body': body, 'move': move if move is not None else stats[7],
        'stats': dict(zip(keys, stats)), 'skills': parse_skills(skills, paren) if isinstance(skills, str) else skills,
        'weapons': weapons, 'gear': gear, 'cyber': cyber, 'role': role, 'info': info, 'page': page,
    }
    out.npcs.append({k: v for k, v in rec.items() if v not in ('', None)})
    return rec


# ---------------------------------------------------------------- Чёрный хром: таблицы EN-списка и тексты RU-описаний
BC_EN_LISTS = os.path.join(ROOT, 'Rulebook', 'dlc', 'black_chrome_en', '16_Black_Chrome_Lists.md')
_bc = None


def costnum(cell):
    m = re.search(r'(\d[\d,]*)\s*eb', cell)
    return int(m.group(1).replace(',', '')) if m else 0


def bc_lists():
    """{префикс категории: [{'Name':…, …, 'note':…}]} из итоговых таблиц EN-оригинала (стр. 158–168); строки-примечания приклеены к предыдущей."""
    global _bc
    if _bc is None:
        _bc, cur, cols = {}, None, None
        for line in open(BC_EN_LISTS, encoding='utf-8'):
            line = line.rstrip('\n')
            m = re.match(r'\*\*▶ (.+?)\*\*', line)
            if m:
                cur, cols = m.group(1), None
                _bc[cur] = []
                continue
            if cur and line.startswith('|') and not line.startswith('|---'):
                cells = [c.strip() for c in line.strip().strip('|').split('|')]
                if cells[0] == 'Name':
                    cols = cells
                    continue
                if cols is None:
                    continue
                if cells[0] == '':
                    note = ' '.join(c for c in cells[1:] if c)
                    note = re.sub(r'^(Upgrades/Notes|Modes/Notes|Notes|Mod\w*/Notes|Extras): ?', '', note)
                    _bc[cur][-1]['note'] = note
                else:
                    _bc[cur].append(dict(zip(cols, cells)))
    return _bc


def bc_cat(prefix):
    for k, v in bc_lists().items():
        if k.startswith(prefix):
            return v
    raise KeyError(prefix)


def bc_row(prefix, name):
    for r in bc_cat(prefix):
        if r['Name'].replace(' ', '') == name.replace(' ', ''):
            return r
    raise KeyError('%s / %s' % (prefix, name))


_KEEP_LEFT = {'по', 'из', 'за', 'во', 'в', 'на', 'со', 'что', 'кто', 'как', 'где', 'когда', 'кое', 'какой', 'чей', 'куда', 'полу', 'пол', 'нео', 'анти', 'мини', 'микро', 'мега', 'био', 'кибер', 'супер', 'экзо', 'мото', 'тех', 'экстра', 'ИИ', 'би'}
_KEEP_RIGHT = {'то', 'либо', 'нибудь', 'таки', 'ка', 'за', 'под', 'ка'}


def dehyph(s):
    def sub(m):
        a, b = m.group(1), m.group(2)
        if a.lower() in _KEEP_LEFT or b in _KEEP_RIGHT:
            return m.group(0)
        return a + b
    s = re.sub(r'([А-Яа-яЁё]{2,})-\s?([а-яё]{2,})', sub, s)
    return s


def ru_text(kw, limit=900, skip=1, drop=(), take=None):
    """Правила предмета из русского описания: абзацы без цены, без служебных строк, перенос слов склеен.
    skip — сколько вводных (рекламных) абзацев пропустить; take — явный список индексов абзацев."""
    e = find('bc_ru', kw, kind='item')
    paras = [p.strip() for p in e['text'].split('\n\n') if p.strip()]
    paras = [p for p in paras if not p.startswith('>') and not re.match(r'^(Забрать или посмотреть|Pick-up or Browse)', p)
             and not re.match(r'^\d[\d,]*eb', p) and 'КИБЕРИМПЛАНТЫ' not in p and 'ещё категории' not in p and p not in ('Экзоскелеты', 'Киберимпланты', 'Поиск') and not re.match(r'^[\d ]+$', p)]
    if take is not None:
        paras = [paras[i] for i in take if i < len(paras)]
    else:
        paras = paras[skip:] if len(paras) > skip else paras
    out = ' '.join(paras)
    out = bc_terms(dehyph(re.sub(r'\s+', ' ', out))).strip()
    out = re.sub(r'\s*Забрать или посмотреть на стенде [\w/]+\.?', '', out).replace('Фотография каталога от TekT', '').strip()
    for d in drop:
        out = out.replace(d, '')
    if len(out) > limit:
        cut = out.rfind('. ', 0, limit)
        out = out[:cut + 1 if cut > limit // 2 else limit].strip()
    return out


_NUMRE = re.compile(r'(?<![\w.])(\d+(?:[.,]\d+)?)(?![\w])')


def _nums(t):
    t = re.sub(r'\d[\d,]*\s*eb', ' ', t)
    t = re.sub(r'(?i)(?:booth|стенд\w*)\s+\w+(?:/\w+)*', ' ', t)
    t = dehyph(re.sub(r'\s+', ' ', t))
    t = re.sub(r'(?<=\d) (?=\d{3}\b)', '', t)          # «3 000» -> 3000
    t = re.sub(r'(?<=\d),(?=\d{3}\b)', '', t)          # «3,000» -> 3000
    from collections import Counter
    return Counter(m.group(1) for m in _NUMRE.finditer(t))


def bc_numdiff(ru_kw, en_kw):
    """Сверка RU/EN описаний предмета: числа, которые есть только в одном из текстов (список строк, пусто — совпадает)."""
    r = find('bc_ru', ru_kw, kind='item')
    e = find('bc_en', en_kw, kind='item')
    a, b = _nums(r['text']), _nums(e['text'])
    for pg in (r['page'], e['page']):
        a.pop(str(pg), None); b.pop(str(pg), None)
    only_ru, only_en = a - b, b - a
    out = []
    if only_ru: out.append('RU: ' + ', '.join('%s×%d' % kv for kv in only_ru.items()))
    if only_en: out.append('EN: ' + ', '.join('%s×%d' % kv for kv in only_en.items()))
    return out


_TERMS = [
    (r'Легк\w+ Бронекостюм\w*', 'лёгкий арморджек'), (r'Средн\w+ Бронекостюм\w*', 'средний арморджек'),
    (r'Л\. Бронекостюм', 'Лёгкий арморджек'), (r'С\. Бронекостюм', 'Средний арморджек'),
    (r'Взрывозащитн\w+ [Бб]рон\w+', 'осколочный бронежилет'), (r'Взрывозащ\.', 'Осколочный бронежилет'),
    (r'со Смартлинк\w*', 'с интерфейсом умного оружия'), (r'Коннектор\w* Смартлинк\w*', 'интерфейс умного оружия'),
    (r'Смартлинком', 'интерфейсом умного оружия'), (r'Смартлинка', 'интерфейса умного оружия'), (r'Смартлинк\w*', 'интерфейс умного оружия'),
    (r'осколочный бронежилет для Головы', 'осколочный шлем'),
    (r'Умные Очки', 'умные очки'),
]


def bc_terms(s):
    """Приводит термины русского издания Чёрного хрома к терминам русского корника (арморджек, осколочный бронежилет, интерфейс умного оружия)."""
    for pat, rep in _TERMS:
        s = re.sub(pat, rep, s)
    return s


def ru_pick(kw, idx, limit=900, kind='item', drop=()):
    """Как ru_text, но idx — номера абзацев в исходной записи (0 — цена); берём только перечисленные, очищаем так же."""
    if kw.startswith('@'):          # запись ищется по тексту, если заголовок в библиотеке потерян
        e = [x for x in entries('bc_ru') if x['kind'] == kind and kw[1:] in x['text']][0]
    else:
        e = find('bc_ru', kw, kind=kind)
    paras = [p.strip() for p in e['text'].split('\n\n') if p.strip()]
    out = ' '.join(paras[i] for i in idx if i < len(paras))
    out = bc_terms(dehyph(re.sub(r'\s+', ' ', out))).strip()
    out = re.sub(r'\s*Забрать или посмотреть на стенде [\w/]+\.?', '', out).replace('Фотография каталога от TekT', '').strip()
    for d in drop:
        out = out.replace(d, '')
    if len(out) > limit:
        cut = out.rfind('. ', 0, limit)
        out = out[:cut + 1 if cut > limit // 2 else limit].strip()
    return out


_TERMS += [(r'ПЗТ', 'ПСП'), (r'(?<=\w)— ', ' — '), (r'\bэто это\b', 'это')]
