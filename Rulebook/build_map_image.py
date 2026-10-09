# -*- coding: utf-8 -*-
"""Склеивает карту Найт-Сити с разворота (с. 312–313 русского издания, индексы 312 и 313) в Content/night_city_map.jpg.
Нужны PyMuPDF и Pillow; PDF лежит в корне репозитория (в git не входит, как и Content/)."""
import io
import os

import fitz
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PDF = os.path.join(ROOT, 'Основная книга правил Cyberpunk Red v1.3.pdf')
OUT = os.path.join(ROOT, 'Content', 'night_city_map.jpg')
DPI = 250

doc = fitz.open(PDF)
parts = []
for i in (312, 313):
    pm = doc[i].get_pixmap(dpi=DPI, alpha=False)
    parts.append(Image.open(io.BytesIO(pm.tobytes('png'))).convert('RGB'))
full = Image.new('RGB', (parts[0].width + parts[1].width, parts[0].height))
full.paste(parts[0], (0, 0))
full.paste(parts[1], (parts[0].width, 0))
full = full.transpose(Image.ROTATE_270)   # на развороте текст лежит боком: поворачиваем по часовой
os.makedirs(os.path.dirname(OUT), exist_ok=True)
full.save(OUT, quality=86, optimize=True)
print(full.size, '->', OUT)
