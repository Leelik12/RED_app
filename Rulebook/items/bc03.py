# -*- coding: utf-8 -*-
"""Чёрный хром (RU, сверка с EN): воздушный и наземный транспорт.
ПЗТ, места, боевая скорость, цена, страница и доступ кочевников — из итоговых таблиц EN (RU p.162–163 совпадает);
состав комплектации — из русских описаний (ru_pick: номера исходных абзацев)."""
from common import *

AIR, GROUND = 'Воздушный', 'Наземный'

# EN-название в таблице, RU-название, ключ RU-записи (под одним ключом иногда несколько машин), абзацы, тип
VEHICLES = [
    ('SH-45 Patroller', 'СХ-45 Патрульный', 'СХ-45 Патрульный Закон', [2], AIR),
    ('SH-45 Patroller Law', 'СХ-45 Патрульный Закон', 'СХ-45 Патрульный Закон', [4], AIR),
    ('Dayton TDT 004', 'Дейтон ТДТ 004', 'Дейтон ТДТ 004', [3], AIR),
    ('Tanson Bellhop', 'Тансон Посыльный', 'Тансон Посыльный', [4], AIR),
    ('Zetatech AeroVox', 'Зетатех АэроВокс', 'Зетатех АэроКоп', [2], AIR),
    ('Zetatech AeroCop', 'Зетатех АэроКоп', 'Зетатех АэроКоп', [4], AIR),
    ('Zetatech Destination', 'Зетатех Дестинейшн', 'Зетатех Дестинейшн', [4], AIR),
    ('Zetatech Herakles', 'Зетатех Геракл', 'Зетатех Геракл', [3], AIR),
    ('AmeriCar EconoCompact', 'АмериКар ЭконоКомпакт', 'АмериКар ЭконоКомпакт', [2], GROUND),
    ('AmeriCar Family Star Van', 'АмериКар Фургон «Фэмили Стар»', 'АмериКар Фургон', [2], GROUND),
    ('Diego Motors Chupacabra', 'Диего Моторс «Чупакабра»', 'Диего Моторс Чупакабра', [3], GROUND),
    ('Diego Motors Range Trike', 'Диего Моторс «Длинный Трайк»', 'Диего Моторс Длинный', [2], GROUND),
    ('The Grundy', 'Гранди «Утиль»', 'Гранди Утиль', [3], GROUND),
    ('The Harvey 100', 'Харви 100', 'Харви Мерк', [3], GROUND),
    ('The Harriet 100', 'Гарриет 100', 'Харви Мерк', [5], GROUND),
    ('The Harvey Merc', 'Харви Мерк', 'Харви Мерк', [7], GROUND),
    ('Highway Master Artemis', 'Владыка Дорог «Артемида»', 'Владыка Дорог', [2], GROUND),
    ('Kaukaz Volkhov', 'Кавказ «Волхов»', 'Волхов Кавказ', [2], GROUND),
    ('Makigai Ebi', 'Макигай «Эби»', 'Макигай Эби', [2], GROUND),
    ('Militech Gorgon Security Van', 'Милитех: охранный фургон «Горгона»', 'Охранный Фургон', [2], GROUND),
    ('The Zacatzontli Pickup Truck', 'Пикап «Закацонтли» («Зак»)', 'Пикап', [2], GROUND),
    ('The Paladin 500', 'Паладин 500', 'Паладин 500', [3], GROUND),
    ('Quadra Thunder-X', 'Квадра «Гром-Х»', 'Квадра Гром', [4], GROUND),
    ('Street King Combat Vehicle', 'Боевая машина «Уличный король»', 'Боевая Машина', [3], GROUND),
    ('Tanson JetBoy Hoverboard', 'Ховерборд «ДжетБой»', 'Тансон ДжетБой', [2], GROUND),
    ('TetraCorp America MegaHauler', 'ТетраКорп Америка: «МегаТягач»', 'ТетраКорп Америка МегаТягач', [3], GROUND),
    ('TetraCorp America Badger Corporate Bus', 'ТетраКорп Америка: корпоративный автобус «Барсук»', 'Корпоративный Автобус', [3], GROUND),
    ('Yang’s Wheels Rickshaw', 'Рикша «Колёса Янга»', 'Колеса Янга Рикша', [3], GROUND),
    ('Zonda Molly 1K', 'Зонда «Молли 1К»', 'Зонда Молли', [3], GROUND),
    ('Zonda Parallax Cybercycle™', 'Зонда «Параллакс Киберцикл»', 'Зонда Параллакс', [3], GROUND),
    ('Zonda Sliver', 'Зонда «Сильвер»', 'Зонда Сильвер', [3], GROUND),
]


def build(out):
    P = 'bc'
    e = find('bc_ru', 'Using Black Chrome', kind='section')
    rows = {r['Name']: r for r in bc_cat('Air') + bc_cat('Land') if 'SDP' in r}
    for en, ru, kw, idx, kind in VEHICLES:
        r = rows[en]
        mph = re.match(r'(\d+) MPH/\s*(\d+) KPH', r['Speed (Narrative)'])
        nomad = r['Nomad Access'] if r['Nomad Access'].isdigit() else ''
        info = ru_pick(kw, idx, 900)
        if en == 'Tanson JetBoy Hoverboard':     # в русском издании 30 миль/ч записаны как 30 км/ч; в оригинале и в таблице 30 MPH (48 км/ч)
            info = info.replace('составляет 30 км/ч', 'составляет 48 км/ч (30 миль/ч)')
        out.add(P, 'vehicle', ru, cost=costnum(r['Cost']),
                Info=info + (' Доступ кочевников: %s.' % nomad if nomad else ' Кочевники не могут модернизировать.'), Lib=e['id'], Page=int(r['Page']),
                Extra={'nomad': nomad, 'hp': r['SDP'], 'seats': r['Seats'], 'combat': re.match(r'\d+', r['Speed (Combat)']).group(0),
                       'narrative': '%s км/ч' % mph.group(2), 'kind': kind})
