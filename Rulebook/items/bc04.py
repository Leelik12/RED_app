# -*- coding: utf-8 -*-
"""Чёрный хром (RU, сверка с EN): взрывчатка, холодное и метательное оружие, огнестрельное оружие.
Навык, урон, магазин, СКОР, руки, скрываемость, цена и страница — из итоговых таблиц EN (RU p.164–166 совпадает);
правила — из русских описаний (ru_pick: номера исходных абзацев). Оружие экзотическое: качество зашито в предмет."""
from common import *

SKILL = {'Shoulder Arms': 'Тактическое оружие', 'Handgun': 'Пистолеты', 'Archery': 'Стрельба из лука', 'Autofire': 'Автоогонь',
         'Heavy Weapons': 'Оружие крупного калибра', 'Melee': 'Оружие ближнего боя', 'Athletics': 'Атлетика'}
AMMO = {'Rifle': 'винтовочные пули', 'H Pistol': 'пули т. пистолета', 'M Pistol': 'пули ср. пистолета', 'VH Pistol': 'пули от. пистолета',
        'Slug': 'жакан', 'Shells': 'дробь', 'Rocket': 'ракеты', 'Grenade': 'гранаты', 'Rocks': 'камни'}

# (EN-название строки, RU-название, ключ RU-записи, абзацы, группа, пометки Alt)
EXPLOSIVES = [
    ('Arachnid Grenade', 'Граната «Арахнид» (Центурион Эссеншиалс)', 'Граната Арахнид', [3, 4], 'Гранаты'),
    ('Shuriken Tornado Grenade', 'Граната «Торнадо сюрикенов»', 'Граната Торнадо', [2], 'Гранаты'),
    ('KTech Security Grenade', 'Охранная граната КТех', 'КТех Охранная граната', [3], 'Гранаты'),
    ('GunMart Door Cracker', 'Дверной взломщик ГанМарт', 'Дверной Взломщик', [2, 3], 'Взрывчатка'),
    ('Micro Hydrogen Combuster', 'Водородный микро-компрессор Гидросубсидиум', 'Водородный Микро', [2], 'Взрывчатка'),
    ('KillChip 100 Micro-Bomb', 'Микро-бомба «УбивающаяЩепка 100»', 'Микро-Бомба', [2], 'Взрывчатка'),
    ('OUTLet Explosive', 'Взрывной АДаптер', 'Взрывной АДаптер', [3, 4], 'Взрывчатка'),
    ('TR-4 Detonator Fluid', 'Детонационная жидкость ТР-4', 'ТР-4', [2], 'Взрывчатка'),
]

# (EN-название, RU-название, ключ RU-записи, абзацы, Alt)
MELEE = [
    ('Arasaka Weeping Reaver Katana', 'Катана «Плачущий разбойник» Арасака', 'Катана Плачущий', [2, 3, 4, 5], 'Покрытие жидкостями Арасаки'),
    ('Kendachi Mono-Katana', 'Моно-катана Кендаши', 'Кендаши Моно-Катана', [3, 4], 'Игнорирует броню ниже ОС 7'),
    ('Solo Wolf and Bot Mono-Katana', 'Моно-катана «Волк Соло и Бот»', 'Волк Соло', [2], 'Подделка'),
    ('Kendachi Mono- Wakizashi', 'Моно-вакидзаси Кендаши', 'Кендаши Моно-Вакидзаси', [2, 3], 'Игнорирует броню ниже ОС 7'),
    ('White Hornet Tanto', 'Танто «Белый шершень»', 'Белый шершень', [4, 5], 'Можно не убивать'),
    ('Rostović Kleaver', 'Кливер Ростовича', 'Ростовиć Кливер', [2, 3, 4], 'Заряжаемый, ТЕЛ 11+'),
    ('Zhirafa Rhinocefist', 'Кулак Носорога (Жирафа)', 'Кулак Носорога', [3], 'Нужны Личные Порты'),
    ('Kendachi Mono-Guard', 'Моно-защита Кендаши', 'Кендаши Моно-Защита', [2], 'Второе критическое'),
    ("Faisal’s Magna Knuckles", 'Магна-кастет Фейсала', 'Магна-Кастет', [2], 'Отключает киберимпланты, нужна батарея'),
    ('The Pursuit Security Bouncer', 'Вышибала (Охрана и Преследование)', 'Вышибала', [3, 4], 'Шокер и микроволновка, нужна батарея'),
    ('SlamDance FangFist', 'СлэмДэнс «Клыкастый кулак»', 'Клыкастый Кулак', [4, 5, 6], 'Лезвие выдвигается'),
    ('SlamDance Tasmanskiy Klô', 'СлэмДэнс «Тасманский коготь»', 'Тасманский', [3, 4, 5, 6], 'Боевая перчатка, +2 к Допросу'),
]
THROWN = [
    ('Ranger Combat Boomerang', 'Боевой бумеранг «Рейнджер»', 'Боевой Бумеранг', [3, 4], 'Возвращается, вдвое режет ОС'),
    ('Utility Tomahawk', 'Утилитарный томагавк', 'Утилитарный Томагавк', [2], 'Метательный, вдвое режет ОС'),
    ('Kendachi Mono-Star', 'Моно-звезда Кендаши', 'Кендаши Моно-Звезда', [2, 3], 'Метательный, второе критическое'),
]
FLUIDS = [
    ('Arasaka Acid', 'Кислота Арасаки', 4), ('Arasaka Fire', 'Огонь Арасаки', 3), ('Arasaka Wound Salt', 'Соль на рану Арасаки', 5),
]

# (EN-название, RU-название, ключ RU-записи, абзацы, Alt)
FIREARMS = [
    ('Arasaka Prototype Variable AR', 'Арасака: прототип изменяемой штурмовой винтовки', 'Арасака Прототип', [3, 4], 'Рейлган, автоогонь, подавляющий огонь'),
    ('E-TACK Public Defender', 'E-TACK «Народный защитник»', 'E-TACK', [2], 'Умный интерфейс, нелетальный режим'),
    ('Eagletech Survivalist', 'Иглтех «Выживальщик»', 'Иглтех', [3], 'Арбалет и винтовка, оптический прицел'),
    ('Faisal’s Dead or Alive', 'Мастерская Фейсала: «Живым или мёртвым»', 'Живым или Мёртвым', [2, 3], 'Дробовик и сетемёт на 2 сети'),
    ('Federated Arms Pepper Shaker', 'Федерейтед Армс «Перечница»', 'Перечница', [3], 'Только автоогонь, подавляющий огонь'),
    ('Georgia Arms Matchmaker', 'Джорджия Армс «Сваха»', 'Сваха', [3], 'Один патрон'),
    ('GunMart Bubba Buster', 'ГанМарт «Трах-бабах»', 'Трах Бабах', [2], 'Может разорваться'),
    ('GunMart Engage Rocket Launcher', 'ГанМарт: ракетная установка «Красотка»', 'Ракетная Установка', [4, 5], 'Взрывы, заряжена, может взорваться'),
    ('GunMart Smart Special', 'ГанМарт «Умный Спешл»', 'ГанМарт Умный', [2], 'Умный интерфейс, может сломаться'),
    ('GunMart Special', 'ГанМарт «Спешл»', 'ГанМарт Спешл', [4], 'Может сломаться'),
    ('Hades Multipurpose Assault Shotgun', 'МетаКорп «Аид» (многоцелевой штурмовой дробовик)', 'Аид', [4], '5 видов боеприпасов одновременно'),
    ('KTech TechHammer', 'КТех «ТехноМолот»', 'КейТех', [3], 'Дробовик и ракетница'),
    ('Midnight Arms Beast Shotgun', 'Миднайт Армс «Зверь»', 'Дробовик Миднайт', [3, 4], 'ТЕЛ 10+ или упор, долгая перезарядка'),
    ('Militech Fox Dual Ammo Pistol', 'Милитех «Лиса» (двузарядный пистолет)', 'Двузарядный', [3, 4], 'Два магазина'),
    ('Militech Mastiff SMG', 'Милитех «Мастиф» (ПП)', 'ПП Мастиф', [2], 'ПП и дробовик'),
    ('Militech Perseus', 'Милитех «Персей»', 'Милитех Персей', [4], 'Скорострельность растёт'),
    ('ModFire 10X', 'МодФайр 10X', 'МодФайр', [2], 'Три режима: пистолет, ПП, винтовка'),
    ('Nomad Rocker', 'Камнемёт кочевников', 'Камнемёт', [2], 'Стреляет камнями'),
    ('Overlord Handcannon', 'ГанМарт: ручная пушка «Оверлорд»', 'Ручная пушка', [3, 4], 'Запугивание'),
    ('Pursuit Security Inc. Crowd Buster', 'Охрана и Преследование: «Разгонятель толп»', '@Разгонятель Толп от Охрана', [2], 'Звуковой режим, автоогонь'),
    ('Rostović Ulični Uništitelj', 'Ростович «Ulični Uništitelj» («Разрушитель улиц»)', 'Ростовиć Ulični', [4, 5], 'Три ствола, металлолом'),
    ('Sanroo Hello Cutie Ultra-K8 Assault Pistol', 'Санру «Хелоу Кьюти» Ультра-K8 (штурмовой пистолет)', 'Санру', [2], 'Пистолет и ПП'),
    ('Sternmeyer M-04 Variable Assault', 'Штейнмейер M-04 «Изменяемая штурмовка»', 'Штейнмейер', [3, 4], 'Винтовка и гранатомёт'),
    ('Superchrome® Glam Rifle', 'Суперхром® «Гламурная винтовка»', 'Суперхром® Гламурная', [3, 4], 'Автоогонь, +2 к Гардеробу и стилю'),
    ('Superchrome® Javelin', 'Суперхром® «Джевелин»', 'Суперхром® Джевелин', [3, 4], '+2 к Гардеробу и стилю'),
    ('Superchrome® Sidearm', 'Суперхром® «Запасное оружие»', 'Суперхром® Запасное', [3, 4], '+2 к Гардеробу и стилю'),
    ('Tommyknocker', 'Центурион «Стукач Томми»', 'Стукач Томми', [2], 'Пистолет и дробовик'),
    ('Westwood', 'Нова Армс «Вествуд»', 'Вествуд', [3], 'Выдвижной экран'),
]

BMG_INFO = ('Стандартное оружие охраны СовОил, ставится на наземную технику и вертолёты СовОил. Экзотическая штурмовая винтовка, при одиночном огне стреляет по навыку '
            '«Оружие крупного калибра». Огромный ленточный магазин на 500 патронов подаётся магнитной рукой сам. Для стрельбы нужно ТЕЛ 11+, если оружие не установлено. '
            'Действием раскладывается сошка: оружие считается установленным, пока его не сдвинут (сошка убирается автоматически). Можно ставить на крепление для тяжёлого вооружения транспорта.')


def anyrow(name):
    for k, v in bc_lists().items():
        for r in v:
            if r['Name'].replace(' ', '') == name.replace(' ', ''):
                return r
    raise KeyError(name)


def skills(s):
    parts = [p.strip() for p in re.split(r'/| or ', s) if p.strip()]
    return ' / '.join(SKILL[p] for p in parts)


def firstint(s, default=1):
    m = re.search(r'\d+', s)
    return int(m.group(0)) if m else default


def mag(s):
    for m in re.finditer(r'(\d+)\s*\(([^)]+)\)', s):
        return int(m.group(1)), AMMO.get(m.group(2).strip(), m.group(2).strip())
    return 0, ''


def build(out):
    P = 'bc'
    lib = {'Lib': find('bc_ru', 'Using Black Chrome', kind='section')['id']}
    for en, ru, kw, idx, grp in EXPLOSIVES:
        r = anyrow(en)
        dmg = r.get('Damage', '')
        pre = ('Урон %s. ' % dmg) if dmg and dmg != '—' else ''
        out.add(P, 'gear', ru, cost=costnum(r['Cost']), Group=grp, Info=pre + ru_pick(kw, idx, 1000), Page=int(r['Page']), **lib)
    for en, ru, kw, idx, alt in MELEE + THROWN:
        r = anyrow(en)
        thrown = 'Skill' in r
        skill = skills(r['Skill'] if thrown else 'Melee')
        if en == 'The Pursuit Security Bouncer':
            skill, dmg, rof, hands = 'Оружие ближнего боя / Пистолеты', '2d6', 2, '1'
        else:
            dmg, rof, hands = r['Damage'], firstint(r['ROF']), str(firstint(r['Hands']))
        out.add(P, 'weapon', ru, cost=costnum(r['Cost']), Skill=skill, Damage=dmg, Rof=rof, Hands=hands, Concealable=r['Concealed?'].startswith('Yes'),
                Melee=(en != 'Ranger Combat Boomerang'), Alt=alt, Info=ru_pick(kw, idx, 1100), Page=int(r['Page']), **lib)
    for en, ru, i in FLUIDS:
        r = anyrow(en)
        out.add(P, 'gear', ru, cost=costnum(r['Cost']), Group='Покрытия для клинков', Info=ru_pick('Катана Плачущий', [i], 500), Page=int(r['Page']), **lib)
    for en, ru, kw, idx, alt in FIREARMS:
        r = anyrow(en)
        m, ammo = mag(r['Magazine'])
        info = BMG_INFO if en == 'Techtronika Russia BMG-500' else ru_pick(kw, idx, 1300)
        out.add(P, 'weapon', ru, cost=costnum(r['Cost']), Skill=skills(r['Skill']), Damage=re.sub(r',? or |, ', ' / ', r['DMG']), Mag=m, AmmoKind=ammo, Rof=firstint(r['ROF']),
                Hands=str(firstint(r['Hands'], 2)) if r['Hands'] != 'Special' else '2', Concealable=r['Concealed?'].startswith('Yes'),
                Alt=alt, Info=info, Page=int(r['Page']), **lib)
    r = anyrow('Techtronika Russia BMG-500')
    m, ammo = mag(r['Magazine'])
    out.add(P, 'weapon', 'Техтроника Россия BMG-500', cost=costnum(r['Cost']), Skill='Оружие крупного калибра', Damage=r['DMG'], Mag=m, AmmoKind=ammo,
            Rof=1, Hands='2', Concealable=False, Alt='Автоогонь, подавляющий огонь, сошка', Info=BMG_INFO, Page=int(r['Page']), **lib)
