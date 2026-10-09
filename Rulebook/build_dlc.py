# -*- coding: utf-8 -*-
"""
Конвертер DLC Cyberpunk RED (PDF из папки DLC/) в Markdown-справочник, так же как корник.

Результат: Rulebook/dlc/
  <NN>_<название>.md   — по одному файлу на каждое небольшое DLC
  black_chrome_en/, black_chrome_ru/ — «Чёрный хром» (170 стр.), разбит на главы как корник
  00_INDEX.md          — список всего и сводка проверки

Что делает (то же, что build_rulebook.py):
  * только текстовый слой, картинки выбрасываются;
  * убирает колонтитулы и декоративную латиницу Dotsies;
  * склеивает переносы и строки абзаца, таблицы -> markdown;
  * перед каждой страницей ставит метку [стр. N | PDF M];
  * проверяет полноту: буквы/цифры страницы должны быть в результате.

Особенность: в нескольких DLC (Погода Найт-Сити, Полночь в Загрузе, Сложные Мини-боссы)
шрифты встроены без таблицы Unicode: текст извлекается как коды глифов. Такие участки
декодируются по закономерности (кириллица = код + 0x2B4, ASCII = код + 0x1D).

Запуск:  py Rulebook/build_dlc.py
Требует: PyMuPDF (pip install pymupdf)
"""
import collections
import io
import os
import re
import sys

import fitz  # PyMuPDF

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_rulebook as core  # noqa: E402

ROOT = core.ROOT
DLC_DIR = os.path.join(ROOT, "DLC")
OUT = os.path.join(ROOT, "Rulebook", "dlc")

# Большие DLC разбираются как корник (по главам из оглавления PDF)
BIG = [
    dict(pdf="DLC/Black Chrome.pdf", out="dlc/black_chrome_en",
         name="Black Chrome (английский оригинал)", cover="cover", intro="Front matter"),
    dict(pdf="DLC/Черный Хром (2).pdf", out="dlc/black_chrome_ru",
         name="Чёрный хром (русский перевод)", cover="обложка", intro="Вступление"),
]

# ----------------------------------------------------------------------------
# Декодирование шрифтов без Unicode-таблицы
# ----------------------------------------------------------------------------
SPECIAL = {chr(0x8F): "°", chr(0x88): "©", chr(0x8A): "«", chr(0x9A): "»"}
GID_RANGE = range(0x14D, 0x19E)
# без этого флага символы без Unicode-таблицы превращаются в U+FFFD и теряются
FLAGS = fitz.TEXT_PRESERVE_WHITESPACE | fitz.TEXT_USE_CID_FOR_UNKNOWN_UNICODE


def has_gids(t):
    return any(ord(c) in GID_RANGE or ord(c) == 0x201 or (ord(c) < 0x20 and c not in "\t\n") for c in t)


def looks_shifted_ascii(t):
    """Чисто ASCII-фрагмент без управляющих кодов: латиница, сдвинутая на 0x1D (строчные -> ЗАГЛАВНЫЕ)."""
    if not t.strip() or any(ord(c) > 0x7F for c in t):
        return False
    if re.search(r"[\\^\]_`{|}~]", t):
        return True
    letters = [c for c in t if c.isalpha()]
    return len(letters) >= 5 and sum(c.isupper() for c in letters) >= 0.9 * len(letters)


def decode_span(t):
    out = []
    for c in t:
        x = ord(c)
        if c == " ":
            out.append(" ")
        elif 0x14D <= x <= 0x19D:
            out.append(chr(x + 0x2B4))
        elif x == 0x201:
            out.append("–")
        elif x < 0x80:
            y = chr(x + 0x1D)
            out.append(SPECIAL.get(y, y))
        else:
            out.append(c)
    s = "".join(out)
    return s.replace("\x1e", "•")


def fix_text(t, font="FuturaPT"):
    # декоративные шрифты (Bisect, OpenSymbol...) и так читаются; ломаются только Futura PT
    if not font.startswith("FuturaPT"):
        return t
    return decode_span(t) if (has_gids(t) or looks_shifted_ascii(t)) else t


# символы, которые в этих шрифтах кодируются не по общей закономерности
FIXUPS = {"Ђ": "Ё", "ѐ": "ё", chr(1): " ", "Û": "", "ȥ": "Σ"}


def fixups(t):
    for a, b in FIXUPS.items():
        t = t.replace(a, b)
    return t


def is_garbage(t):
    """Строка из кодов глифов декоративного шрифта (заголовок страницы) — читать нечего."""
    chars = [c for c in t if not c.isspace()]
    if not chars:
        return True
    odd = sum(1 for c in chars if not (c.isalnum() or c in ".,:;-–—!?()\"'%+/*«»°©•▶◀=&|№…") )
    return odd >= 0.25 * len(chars)


# ----------------------------------------------------------------------------
# Извлечение блоков (копия core.extract_page с декодированием)
# ----------------------------------------------------------------------------
def extract_page(page, removed):
    blocks = []
    for b in page.get_text("dict", flags=FLAGS)["blocks"]:
        if b["type"] != 0:
            continue
        lines = []
        for l in b["lines"]:
            parts, sizes, fonts = [], collections.Counter(), collections.Counter()
            for s in l["spans"]:
                t = s["text"]
                if not t:
                    continue
                if s["font"].startswith(core.NOISE_FONTS):
                    if t.strip():
                        removed.append(("dotsies", t))
                    continue
                t = fix_text(t, s["font"])
                parts.append(t)
                if t.strip():
                    sizes[round(s["size"], 1)] += len(t)
                    fonts[s["font"]] += len(t)
            text = fixups(core.norm_space("".join(parts)))
            if not text.strip():
                continue
            if is_garbage(text):
                removed.append(("garbage", text))
                continue
            size = sizes.most_common(1)[0][0] if sizes else 0
            font = fonts.most_common(1)[0][0] if fonts else ""
            ln = core.Line(text, *l["bbox"], size, font)
            ln.maxsize = max(sizes) if sizes else 0
            lines.append(ln)
        if not lines:
            continue
        sz, fn = collections.Counter(), collections.Counter()
        for ln in lines:
            sz[ln.size] += len(ln.text)
            fn[ln.font] += len(ln.text)
        blocks.append(dict(lines=lines, size=sz.most_common(1)[0][0],
                           font=fn.most_common(1)[0][0], bbox=b["bbox"]))
    return blocks


def reorder(blocks, page_w):
    """Порядок чтения для многоколоночных страниц: полосы между блоками во всю ширину,
    внутри полосы — колонка за колонкой, сверху вниз (в PDF поток блоков часто перемешан)."""
    if len(blocks) < 3:
        return blocks
    wide = lambda b: (b["bbox"][2] - b["bbox"][0]) > 0.6 * page_w
    weights = [(b["bbox"][2] - b["bbox"][0], len("".join(l.text for l in b["lines"]))) for b in blocks]
    narrow_text = sum(n for w, n in weights if w <= 0.6 * page_w)
    if narrow_text < 0.5 * sum(n for w, n in weights):
        return blocks
    ncols = 3 if sum(1 for b in blocks if (b["bbox"][2] - b["bbox"][0]) < 0.36 * page_w) > len(blocks) * 0.5 else 2
    def col(b):
        return min(ncols - 1, int(((b["bbox"][0] + b["bbox"][2]) / 2) / (page_w / ncols)))
    seps = sorted((b for b in blocks if wide(b)), key=lambda b: b["bbox"][1])
    out, rest = [], [b for b in blocks if not wide(b)]
    edges = [b["bbox"][1] for b in seps]
    def band(b):
        y = b["bbox"][1]
        return sum(1 for e in edges if e <= y + 1)
    bands = collections.defaultdict(list)
    for b in rest:
        bands[band(b)].append(b)
    for i in range(len(seps) + 1):
        if i > 0:
            out.append(seps[i - 1])
        out += sorted(bands.get(i, []), key=lambda b: (col(b), b["bbox"][1], b["bbox"][0]))
    return out


def title_of(fname):
    n = os.path.splitext(fname)[0]
    n = re.sub(r"^DLC\s*[—-]\s*", "", n)
    n = re.sub(r"\s*\(\d+\)$", "", n)
    return n.strip()


def build_small(pdf_path, idx, title, index_rows, totals):
    doc = fitz.open(pdf_path)
    slug = core.slug(title)
    fname = "%02d_%s.md" % (idx, slug)
    verify_lines = []
    removed_all = []
    with io.open(os.path.join(OUT, fname), "w", encoding="utf-8") as f:
        f.write("# %s\n\n" % title)
        f.write("_DLC «%s» — файл `%s`, %d стр. Метка [стр. N | PDF M] стоит перед текстом каждой "
                "страницы._\n\n" % (title, os.path.basename(pdf_path), doc.page_count))
        for i, page in enumerate(doc):
            label = (page.get_label() or "").strip()
            label = label if label.isdigit() else str(i + 1)
            removed = []
            blocks = reorder(extract_page(page, removed), page.rect.width)
            lines = core.render_page(blocks, removed, page.rect.height, set(), label,
                                     core.find_tables(page))
            f.write("\n---\n[стр. %s | PDF %d]\n\n" % (label, i + 1))
            if not lines:
                f.write("_(на странице нет текста — только иллюстрации)_\n")
            else:
                f.write("\n\n".join(lines) + "\n")
            removed_all += [(i + 1, k, t) for k, t in removed]
            # проверка полноты: источник считаем после того же декодирования
            src = "".join(core.alnum("".join(fix_text(s["text"], s["font"]) for l in b["lines"] for s in l["spans"]
                                              if not s["font"].startswith(core.NOISE_FONTS)))
                          for b in page.get_text("dict", flags=FLAGS)["blocks"]
                          if b["type"] == 0)
            # номера страниц (колонтитулы) в тексте не нужны — потерей не считаем
            # порядок чтения мог измениться (колонки), поэтому сравниваем наборы букв, а не последовательности
            noise = collections.Counter("".join(core.alnum(t) for k, t in removed))
            got = collections.Counter(core.alnum(chr(10).join(lines)))
            miss = collections.Counter(src) - got - noise
            lost = ["".join(c * n for c, n in miss.items())] if sum(miss.values()) > 3 else []
            totals[0] += len(src)
            totals[1] += sum(len(x) for x in lost)
            if lost:
                verify_lines.append("%s, стр. %s (PDF %d): не найдено %r" % (title, label, i + 1, lost))
    index_rows.append((idx, title, doc.page_count, fname))
    return verify_lines


def main():
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT):
        if f.endswith((".md", ".txt")):
            os.remove(os.path.join(OUT, f))

    big_files = {os.path.basename(c["pdf"]) for c in BIG}
    if "--small" not in sys.argv:   # --small: пропустить «Чёрный хром» (он собирается ~2 минуты)
        for cfg in BIG:
            core.build(cfg)

    files = sorted(f for f in os.listdir(DLC_DIR)
                   if f.lower().endswith(".pdf") and f not in big_files)
    rows, totals, verify = [], [0, 0], []
    for n, f in enumerate(files, 1):
        verify += build_small(os.path.join(DLC_DIR, f), n, title_of(f), rows, totals)

    with io.open(os.path.join(OUT, "_verify_report.txt"), "w", encoding="utf-8") as v:
        v.write("\n".join(verify))
        v.write("\n\nИТОГО букв/цифр в исходнике: %d, потеряно: %d\n" % tuple(totals))

    with io.open(os.path.join(OUT, "00_INDEX.md"), "w", encoding="utf-8") as f:
        f.write("# DLC Cyberpunk RED — текстовые версии\n\n")
        f.write("Источник: PDF в папке `DLC/` (в git не входят). Сборка: `py Rulebook/build_dlc.py`.\n\n")
        f.write("| DLC | страниц | файл |\n|---|---|---|\n")
        f.write("| Чёрный хром (RU) | 170 | `black_chrome_ru/` (по главам) |\n")
        f.write("| Black Chrome (EN) | 170 | `black_chrome_en/` (по главам) |\n")
        for n, title, pages, fname in rows:
            f.write("| %s | %d | `%s` |\n" % (title, pages, fname))
        f.write("\nПроверка полноты: `_verify_report.txt`.\n")
    print("готово: %d DLC, букв %d, потеряно %d" % (len(rows), totals[0], totals[1]))


if __name__ == "__main__":
    main()
