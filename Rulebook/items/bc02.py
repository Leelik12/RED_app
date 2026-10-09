# -*- coding: utf-8 -*-
"""Чёрный хром (RU, сверка с EN): стиль и броня, общие товары и снаряжение, экзоскелеты.
Цены, ОС, штрафы и страницы — из итоговых таблиц EN (RU p.160–162 совпадает); правила — из русских описаний (ru_pick: номера исходных абзацев)."""
from common import *

BORG = 'БОРГИРОВАНИЕ (BORGWARE)'

TYPES = {'Light Armorjack': 'Лёгкий арморджек', 'Medium Armorjack': 'Средний арморджек', 'Kevlar®': 'Кевлар®',
         'Flak': 'Осколочный бронежилет', 'Bodyweight Suit': 'Костюм Bodyweight'}
ZONES = {'Body & Head': 'тело и голова', 'Body or Head': 'тело или голова', 'Body': 'тело', 'Head': 'голова'}

# EN-название, RU-название, ключ RU-записи, абзацы RU (исходные номера), доп. фраза/лимит
ARMOR = [
    ('Corporate Island', 'Корпоративный остров', 'Корпоративный Остров', [4], 900),
    ('The Dirk Combat Jacket', 'Боевая куртка «Кортик»', 'Боевая Куртка Кортик', [3, 4], 900),
    ('Executive Armor', 'Броня директора', 'Броня Директора', [3, 4], 900),
    ('Fire Brand Bunker Gear', 'Бункерное снаряжение «Огненная Марка»', 'Бункерное', [2, 3], 1300),
    ('Laser Light Street Jacket', 'Уличная куртка «Лазерный свет»', 'Уличная Куртка', [4], 900),
    ('Lotos Netsuit', 'Сетевой костюм «Лотос»', 'Сетевой Костюм', [2], 1300),
    ('Masetto AirRider', 'Воздушный наездник «Масетто»', 'Воздушный Наездник', [2], 900),
    ('Mimic Clothing Kit', 'Комплект мимиковской одежды', 'Комплект Мимиковской', [2], 900),
    ('Montage Variable Clothing Line', 'Линия изменяющейся одежды «Монтаж»', 'Линия Изменяющейся', [2], 1000),
    ('Shock Armor', 'Шоковая броня', 'Шоковая Броня', [3], 900),
    ('SkidRow Trench', 'СкидРоу Тренч', 'СкидРоу', [4], 900),
    ('Street Viper Riding Suit', 'Ездовой костюм «Уличная гадюка»', 'Зонда', [2], 900),
    ('Tactical Smart Armor', 'Тактическая умная броня', 'Тактическая Умная', [3], 900),
    ('MechaMan Motorcycle Helmet', 'Мотоциклетный шлем «МехаМэн»', 'МехаМэн Мотоциклетный', [2], 900),
]

# (EN-название строки таблицы, RU-название, ключ RU-записи, абзацы, группа, лимит)
GOODS = [
    ('KillStrom Banshee Microphone', 'Микрофон «Банши» КиллСтром', 'Микрофон Банши', [3], 'Развлечения', 900),
    ('KillStrom Sonic Boom Amp', 'Усилитель «Звуковой удар» КиллСтром', 'Усилитель Звукового', [3], 'Развлечения', 900),
    ('KillStrom Typhoid Speaker', 'Колонка «Тифоид» КиллСтром', 'Колонка', [2], 'Развлечения', 900),
    ('Laser Light Electric Guitar', 'Электрогитара «Лазерный свет»', 'Электрогитара', [2], 'Развлечения', 900),
    ('The Drink Master 3000', 'Мастер напитков 3000', 'Мастер Напитков', [2], 'Образ жизни', 900),
    ('Hydrosubsidium Universal Aqualung', 'Универсальный акваланг Гидросубсидиум', 'Акваланг', [2], 'Образ жизни', 900),
    ('Jeeves Executive Garment Bag', 'Чехол для одежды «Корпорат Дживс»', 'Чехол для Одежды', [3, 4], 'Образ жизни', 900),
    ('KTech Doberman 500 Marking Scent', 'Маркирующий запах для «Добермана 500»', 'Маркирующий Запах', [2], 'Безопасность', 1100),
    ('RapiDeploy Sheath', 'СвобоДоступные ножны', 'СвобоДоступные', [2], 'Безопасность', 900),
    ('Streetcase by SecSystems', 'Уличный кейс СекСистемс', 'Уличный Кейс', [2], 'Безопасность', 1300),
    ('Everest VentureWare AirWell 50', 'Воздушный колодец 50', 'Воздушный Колодец', [2], 'Выживание', 900),
    ('Everest VentureWare One Touch Habitat', 'Жилище в одно касание', 'Жилище в одно', [2], 'Выживание', 1000),
    ('Mr. Biscuit Multi-Food Processor', 'Мультипищевой комбайн «Мистер Бисквит»', 'Мистер Бисквит', [2], 'Выживание', 900),
    ('Petrochem Nitro Ultra9', 'Нитро Ультра9', 'Нитро Ультра9', [2], 'Выживание', 1100),
    ('SovOil Lubricants', 'Лубриканты СовОил', 'СовОил Лубрикант', [1], 'Прочее', 700),
    ('Triti-Fizz', 'Triti-Fizz', 'Triti-Fizz', [1], 'Образ жизни', 600),
]

# имя, ключ RU, EN-строка таблицы линейных рам, внутренний вариант?, абзацы с правилами
EXO = [
    ('Экзоскелет «Фума Котаро»', 'Экзоскелет Фū', 'Fūma Kotarō Linear Frame', [2, 3, 4]),
    ('Экзоскелет ЛФ-001 SWAT', 'Экзоскелет ЛФ', 'LF-001 SWAT Linear Frame', [2, 3, 4]),
    ('Экзоскелет «Вермилион»', 'Экзоскелет Вермилион', 'Vermilion Linear Frame', [2, 3, 4]),
    ('Экзоскелет ЭЛ-Ф4-НТ', 'Экзоскелет ЭЛ', 'EL-F4-NT Linear Frame', [3, 4]),
]


# у этих брони первая фраза описания лишь повторяет тип брони, уже указанный в заголовке
TYPE_SENTENCE = {'Corporate Island', 'The Dirk Combat Jacket', 'Executive Armor', 'Fire Brand Bunker Gear', 'Shock Armor', 'SkidRow Trench'}


def penalty(s):
    m = re.search(r'-(\d+)', s)
    return int(m.group(1)) if m else 0


def build(out):
    P = 'bc'
    e = find('bc_ru', 'Using Black Chrome', kind='section')
    lib = {'Lib': e['id']}
    fa = bc_cat('Fashion') + [r for r in bc_cat('General') if 'Armor Type' in r]    # две брони в таблице списка попали под общие товары
    goods = {r['Name']: r for r in bc_cat('General') if 'Armor Type' not in r}

    def fa_row(en):
        for r in fa:
            if r['Name'] == en:
                return r
        raise KeyError(en)

    for en, ru, kw, idx, lim in ARMOR:
        r = fa_row(en)
        t, zone = re.match(r'(.+?) \((.+)\)$', r['Armor Type']).groups()
        sp = int(r['SP'])
        head = '%s (%s), ОС %d' % (TYPES[t], ZONES[zone], sp)
        pen = penalty(r['Armor Penalty'])
        if pen:
            head += ', штраф −%d к РЕА, ЛВК и СКО' % pen
        look = r['Appearance Of']
        if look not in ('—', 'None'):
            head += '. Выглядит как: ' + look
        body = ru_pick(kw, idx, lim)
        if en in TYPE_SENTENCE:
            body = re.sub(r'^[^.]*? — это [^.]*\.\s*', '', body)
        info = head + '. ' + body if body else head + '.'
        out.add(P, 'armor', ru, cost=costnum(r['Cost']), SP=sp, Penalty=pen, Suit=(zone == 'Body & Head'), Info=info,
                Page=int(r['Page']), **lib)
    # Голо-одежда и перчатка МехаМэн — не броня
    r = fa_row('Masetto Holo-Wear')
    out.add(P, 'gear', 'Голо-одежда «Масетто»', cost=costnum(r['Cost']), Group='Одежда',
            Info=ru_pick('Голо-Одежда', [3, 4], 1100), Page=int(r['Page']), **lib)
    r = fa_row('MechaMan Smart Glove')
    out.add(P, 'gear', 'Умная перчатка «МехаМэн»', cost=costnum(r['Cost']), Group='Одежда',
            Info=ru_pick('МехаМэн Умная', [3], 1000).replace('с интерфейсом умного оружия', 'с предустановленным подкожным захватом')
                     .replace('использования интерфейса умного оружия', 'использования подкожного захвата'), Page=int(r['Page']), **lib)

    for en, ru, kw, idx, grp, lim in GOODS:
        r = goods[en]
        out.add(P, 'gear', ru, cost=costnum(r['Cost']), Group=grp, Info=ru_pick(kw, idx, lim), Page=int(r['Page']), **lib)
    # записи, слипшиеся с соседними в русской библиотеке
    r = goods['ChipVault by SecSystems']
    cv = find('bc_ru', 'Порта-Принтер', kind='item')['text']
    cv = cv[cv.index('ЩепкоХранилище вмещает'):]
    out.add(P, 'gear', 'ЩепкоХранилище СекСистемс', cost=costnum(r['Cost']), Group='Образ жизни',
            Info=bc_terms(dehyph(re.sub(r'\s+', ' ', cv))).strip(), Page=int(r['Page']), **lib)
    r = goods['Shower•in•a•Can']
    out.add(P, 'gear', 'Д•В•Б (Душ•В•Банке)', cost=costnum(r['Cost']), Group='Образ жизни',
            Info='Пена для дезинфекции и дезодорации в баллончике: распылить, вытереть — и готово. Цвета «Классическая чистота» и «Свежая сосна». Один баллончик — одно использование.',
            Page=int(r['Page']), **lib)
    r = goods['KTech Doberman 500']
    dob = ('Четвероногий боевой дрон среднего размера: СКО 8, ОС 11, ПЗ 25, боевой номер 10, дальность 50 м, нельзя перехватить. Оснащён экзотическим очень тяжёлым ОББ, детектором движения и обонятельным комплектом. '
           + ru_pick('КТех Доберман 500 КТех', [6, 7, 8, 9, 10, 11], 3400))
    out.add(P, 'gear', 'Боевой дрон КТех «Доберман 500»', cost=costnum(r['Cost']), Group='Безопасность',
            Info=dob, Page=int(r['Page']), Extra={'type': 'drone'}, **lib)
    # Порта-Принтер и АэроСфера в итоговой таблице общих товаров не перечислены: цена и страница — из самих записей
    for kw, ru, grp, idx, lim in (('Порта-Принтер', 'Порта-Принтер Зетатех', 'Образ жизни', [2, 3], 900),
                                  ('ВорлдСат', 'АэроСфера ВорлдСат', 'Выживание', [2, 3], 1500)):
        pe = find('bc_ru', kw, kind='item')
        out.add(P, 'gear', ru, cost=costnum(pe['text']), Group=grp, Info=ru_pick(kw, idx, lim), Page=find('bc_en', 'Porta-Printer' if kw == 'Порта-Принтер' else 'WorldSat Aerial', kind='item')['page'], **lib)

    # экзоскелеты: внутренний вариант — боргирование (кибер), внешний — снаряжение
    lf = {r['Name']: r for r in bc_cat('Linear')}
    for name, kw, en, idx in EXO:
        r = lf[en]
        cost = costnum(r['Cost'])
        info = ru_pick(kw, idx, 1500).replace('Этот экзоскелет можно приобрести как во внешнем, так и во внутреннем вариантах. В любом случае, при правильном подключении к экзоскелету, ',
                                              'При правильном подключении к экзоскелету ')
        if r['Internal Install'] != 'N/A':
            ext = 'Для работы Внешнего Экзоскелета требуются два установленных Личных Порта, и его можно включать и выключать с помощью Действия.'
            inner = info.replace(ext, '').replace('  ', ' ').strip()
            outer = re.sub(r'\s*Установка Внутреннего Экзоскелета.*$', '', info).strip()
            out.add(P, 'cyber', name + ' (внутренний)', cost=cost, HL=r['Internal HL'], Install='Госпиталь', Category=BORG, Slots=0,
                    Info='Боргирование. ' + inner, Page=int(r['Page']), **lib)
            out.add(P, 'gear', name + ' (внешний)', cost=cost, Group='Экзоскелеты', Info=outer, Page=int(r['Page']), **lib)
        else:
            out.add(P, 'gear', name, cost=cost, Group='Экзоскелеты', Info=info, Page=int(r['Page']), **lib)
