# -*- coding: utf-8 -*-
"""12 Дней Киберждества: киберимпланты и щепка."""
from common import *

ARM = 'ПРАВАЯ КИБЕРРУКА (CYBERARM R)'
LEG = 'ПРАВАЯ КИБЕРНОГА (CYBERLEG R)'
NEURAL = 'НЕЙРОИНТЕРФЕЙС (NEURALWARE)'
EXTERNAL = 'ВНЕШНИЕ ИМПЛАНТЫ (EXTERNAL)'
BORG = 'БОРГИРОВАНИЕ (BORGWARE)'

# (заголовок в библиотеке, имя, категория, слотов, прочее)
ITEMS = [
    ('ЦЕПНОЙ ПОТРОШИТЕЛЬ', 'Цепной Потрошитель', ARM, 4, {}),
    ('ГЭНГ ДЖАЗЗЛЕР', 'Киберматрикс Гэнг Джаззлер', ARM, 1, {}),
    ('ЖУЧИНЫЙ ГЛАЗ', 'Циклоп Интернешнл Жучиный Глаз', BORG, 1, {}),
    ('ТРАНС СМЕРТИ', 'Транс Смерти', NEURAL, 1, {}),
    ('МОНОВИЗОР', 'Кироши МоноВизор', BORG, 1, {}),
    ('ОПТИЩИТ', 'Кироши Оптищит', EXTERNAL, 1, {}),
    ('УОТЧМЭН', 'Псайберстафф УотчМэн', ARM, 1, {}),
    ('ДУГОМЕТ', 'Радлайн Киберрука-Дугомет «Блицкриг»', ARM, 0, {}),
    ('МИКРОВАЛДО', 'Рэйвен Микрокибернетикс МикроВалдо', ARM, 1, {}),
    ('ПЛЕТЕНАЯ КОЖА', 'Сайкаст Плетёная Кожа', EXTERNAL, 1, {}),
    ('КИБЕРНОГИ РОМАНОВОЙ', 'Визард Технолоджис Киберноги Романовой', LEG, 0, {'Paired': True}),
]


def build(out):
    out.pack('dlc01', '12 Дней Киберждества', '12 Days of Cybermas')
    for key, name, cat, slots, extra in ITEMS:
        e = find('dlc01', key, 'item')
        t = e['text']
        out.add('dlc01', 'cyber', name, cost=parse_cost(t), Install=parse_install(t), HL=parse_hl(t), Category=cat,
                Slots=slots, Info=body_info(t), Lib=e['id'], Page=e['page'], **{k.lower() if False else k: v for k, v in extra.items()})
    # щепка «Позер Чип» продаётся как расходник
    e = find('dlc01', 'ПОЗЕР ЧИП', 'item')
    out.add('dlc01', 'gear', 'Позер Чип (щепка)', cost=500, Group='Щепки', Info=body_info(e['text']), Lib=e['id'], Page=e['page'])
