# -*- coding: utf-8 -*-
"""
Конвертер корника Cyberpunk RED (PDF) в упрощённый Markdown-справочник.

Что делает:
  * берёт только текстовый слой (картинки выбрасываются);
  * убирает колонтитулы (название главы / номер страницы) и декоративный
    латинский шрифт Dotsies ("s k i l l l i s t ..."), всё убранное пишется в лог;
  * склеивает переносы (мягкий перенос U+00AD), строки абзаца, буквицы;
  * строки таблиц (ячейки на одной высоте) склеивает через " | ";
  * перед каждой страницей ставит метку [стр. N | PDF M]:
      N — номер, напечатанный на странице книги, M — номер страницы в просмотрщике PDF.
  * раскладывает текст по главам (по оглавлению PDF) + индекс.

Проверка полноты: каждое слово исходной страницы должно оказаться в выходной
странице (кроме залогированного мусора). Отчёт — в _verify_report.txt.

Запуск:  py Rulebook/build_rulebook.py          # обе книги
         py Rulebook/build_rulebook.py ru       # только русский перевод v1.3
         py Rulebook/build_rulebook.py en       # только английский оригинал
Требует: PyMuPDF (pip install pymupdf)
"""
import collections
import io
import os
import re
import sys
import unicodedata

import fitz  # PyMuPDF

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BOOKS = {
    "ru": dict(pdf="Основная книга правил Cyberpunk Red v1.3.pdf", out="ru",
               name="Корник Cyberpunk RED v1.3 (русский перевод)",
               cover="обложка", intro="Вступление и содержание"),
    "en": dict(pdf="cyberpunkredcorerulebook.pdf", out="en",
               name="Cyberpunk RED Core Rulebook (английский оригинал)",
               cover="cover", intro="Front matter"),
}

SOFT = "­"
NOISE_FONTS = ("Dotsies",)                 # декоративная латиница
HEADER_FONT = "BisectRegular"              # колонтитулы: глава (19pt) и номер страницы (18pt)
HEADING_FONTS = ("BlenderPro", "BisectRegular", "DangerNeueRough", "Refinery", "Marske")
SIDEBAR_SIZE = 8.0                         # врезки ИНФО и подписи на полях

def norm_space(s):
    return s.replace("\t", " ").replace(" ", " ")


def page_label(doc, i, cover="обложка"):
    lab = (doc[i].get_label() or "").strip()
    return lab if lab.isdigit() else cover


# ----------------------------------------------------------------------------
# Разбор страницы на логические блоки
# ----------------------------------------------------------------------------
class Line:
    def __init__(self, text, x0, y0, x1, y1, size, font):
        self.text, self.x0, self.y0, self.x1, self.y1 = text, x0, y0, x1, y1
        self.size, self.font = size, font


def extract_page(page, removed):
    """Возвращает список блоков: dict(lines=[Line], size, font, bbox)."""
    blocks = []
    for b in page.get_text("dict", flags=fitz.TEXT_PRESERVE_WHITESPACE)["blocks"]:
        if b["type"] != 0:
            continue  # картинка
        lines = []
        for l in b["lines"]:
            parts, sizes = [], collections.Counter()
            fonts = collections.Counter()
            for s in l["spans"]:
                t = s["text"]
                if not t:
                    continue
                if s["font"].startswith(NOISE_FONTS):
                    if t.strip():
                        removed.append(("dotsies", t))
                    continue
                parts.append(t)
                if t.strip():
                    sizes[round(s["size"], 1)] += len(t)
                    fonts[s["font"]] += len(t)
            text = norm_space("".join(parts))
            if not text.strip():
                continue
            size = sizes.most_common(1)[0][0] if sizes else 0
            font = fonts.most_common(1)[0][0] if fonts else ""
            # максимальный кегль — для буквиц
            maxsize = max(sizes) if sizes else 0
            ln = Line(text, *l["bbox"], size, font)
            ln.maxsize = maxsize
            lines.append(ln)
        if not lines:
            continue
        sz = collections.Counter()
        fn = collections.Counter()
        for ln in lines:
            sz[ln.size] += len(ln.text)
            fn[ln.font] += len(ln.text)
        blocks.append(dict(lines=lines, size=sz.most_common(1)[0][0],
                           font=fn.most_common(1)[0][0], bbox=b["bbox"]))
    return blocks


def is_running_header(blk, page_h, chapter_titles, label):
    if not blk["font"].startswith(HEADER_FONT):
        return False
    if not (17.5 <= blk["size"] <= 19.5):
        return False
    y0, y1 = blk["bbox"][1], blk["bbox"][3]
    if not (y1 < 60 or y0 > page_h - 60):
        return False
    txt = " ".join(l.text.strip() for l in blk["lines"]).strip()
    return txt == label or txt.upper() in chapter_titles or txt.isdigit()


def join_lines(lines):
    """Склейка строк блока: переносы, ячейки таблиц, буквицы."""
    out = ""
    prev = None
    for ln in lines:
        t = ln.text
        if prev is None:
            out = t
        else:
            p = out
            same_row = abs(ln.y0 - prev.y0) < 2.5 and ln.x0 > prev.x1 + 8
            dropcap = (len(prev.text.strip()) == 1 and prev.maxsize > 1.5 * max(ln.size, 1)
                       and prev.text.strip().isalpha())
            if dropcap:
                out = p.rstrip() + t.lstrip()
            elif same_row:
                out = p.rstrip() + " | " + t.lstrip()
            elif p.endswith(SOFT):
                out = p[:-1] + t.lstrip()
            elif re.search(r"[A-Za-zА-Яа-яЁё]-$", p.rstrip()) and re.match(r"\s*[a-zа-яё]", t):
                out = p.rstrip() + t.lstrip()
            else:
                out = p.rstrip() + " " + t.lstrip()
        prev = ln
    out = out.replace(SOFT, "")
    return re.sub(r"[ ]{2,}", " ", out).strip()


ENDS_SENTENCE = re.compile(r"[.!?:;…»)\]\"”]$")


def clean_cell(c):
    if c is None:
        return ""
    c = re.sub(SOFT + r"\s*\n\s*", "", c).replace(SOFT, "")
    c = norm_space(c).replace("\n", " ").replace("|", "\\|")
    return re.sub(r"[ ]{2,}", " ", c).strip()


def table_markdown(rows):
    rows = [[clean_cell(c) for c in r] for r in rows]
    rows = [r for r in rows if any(r)]
    ncol = max(len(r) for r in rows)
    rows = [r + [""] * (ncol - len(r)) for r in rows]
    md = ["| " + " | ".join(rows[0]) + " |", "|" + "---|" * ncol]
    md += ["| " + " | ".join(r) + " |" for r in rows[1:]]
    return "\n".join(md)


def find_tables(page):
    """Настоящие таблицы страницы: >=2 строк, >=2 колонок, >=4 непустых ячеек."""
    res = []
    try:
        tabs = page.find_tables().tables
    except Exception:
        return res
    for t in tabs:
        rows = t.extract()
        filled = sum(1 for r in rows for c in r if c and c.strip())
        if t.row_count >= 2 and t.col_count >= 2 and filled >= 4:
            res.append((fitz.Rect(t.bbox), rows))
    return res


def render_page(blocks, removed, page_h, chapter_titles, label, tables=()):
    items = []  # (kind, text, size, font)
    # блоки, попавшие внутрь таблицы, заменяются одной markdown-таблицей,
    # но только если таблица содержит ВЕСЬ их текст (иначе остаёмся на построчном варианте)
    table_of_block = {}
    for ti, (rect, rows) in enumerate(tables):
        r = fitz.Rect(rect.x0 - 2, rect.y0 - 2, rect.x1 + 2, rect.y1 + 2)
        inside = [bi for bi, blk in enumerate(blocks)
                  if bi not in table_of_block
                  and r.contains(fitz.Point((blk["bbox"][0] + blk["bbox"][2]) / 2,
                                            (blk["bbox"][1] + blk["bbox"][3]) / 2))]
        if not inside:
            continue
        blk_a = alnum("".join(l.text for bi in inside for l in blocks[bi]["lines"]))
        tab_a = alnum("".join(clean_cell(c) for row in rows for c in row))
        if blk_a and not lost_fragments(blk_a, tab_a):
            for bi in inside:
                table_of_block[bi] = ti

    emitted_tables = set()
    for bi, blk in enumerate(blocks):
        if is_running_header(blk, page_h, chapter_titles, label):
            removed.append(("header", " ".join(l.text.strip() for l in blk["lines"])))
            continue
        if bi in table_of_block:
            ti = table_of_block[bi]
            if ti not in emitted_tables:
                emitted_tables.add(ti)
                items.append(dict(text=table_markdown(tables[ti][1]), size=0, font="TABLE",
                                  maxsize=0, table=True))
            continue
        text = join_lines(blk["lines"])
        if not text:
            continue
        items.append(dict(text=text, size=blk["size"], font=blk["font"],
                          maxsize=max(l.maxsize for l in blk["lines"])))

    # буквица отдельным блоком: одна буква крупным кеглем -> приклеить к следующему
    merged = []
    i = 0
    while i < len(items):
        it = items[i]
        if (len(it["text"]) == 1 and it["text"].isalpha() and it["maxsize"] >= 20
                and i + 1 < len(items)):
            items[i + 1]["text"] = it["text"] + items[i + 1]["text"]
            i += 1
            continue
        merged.append(it)
        i += 1

    # продолжение абзаца, разорванного на блоки
    final = []
    for it in merged:
        if final:
            pv = final[-1]
            cont = (pv["font"] == it["font"] and abs(pv["size"] - it["size"]) < 0.6
                    and not pv["text"].startswith("▶") and not it["text"].startswith("▶")
                    and " | " not in pv["text"] and " | " not in it["text"]
                    and not ENDS_SENTENCE.search(pv["text"])
                    and re.match(r"[a-zа-яё(«,]", it["text"]))
            if cont:
                pv["text"] = pv["text"] + " " + it["text"]
                continue
        final.append(it)

    out = []
    for it in final:
        t, size, font = it["text"], it["size"], it["font"]
        if it.get("table"):
            out.append(t)
        elif t.startswith("▶") and t.endswith("◀"):
            out.append("#### " + t.strip("▶◀ ").strip())
        elif t.startswith("▶"):
            out.append("**▶ " + t.lstrip("▶ ").strip() + "**")
        elif (font.startswith(HEADING_FONTS) and it["maxsize"] >= 20 and len(t) < 120):
            out.append("## " + t)
        elif (font.startswith(HEADING_FONTS) and it["maxsize"] >= 13 and len(t) < 120):
            out.append("### " + t)
        elif size <= SIDEBAR_SIZE and size > 0:
            out.append("> " + t)
        else:
            out.append(t)
    return out


# ----------------------------------------------------------------------------
# Проверка полноты
# ----------------------------------------------------------------------------
# Сравниваем поток букв и цифр (без пробелов и знаков): склейка переносов, строк
# и буквиц потерей не считается, а любой выпавший символ будет пойман.
import difflib

NON_ALNUM = re.compile(r"[^0-9A-Za-zА-Яа-яЁё]+")


def alnum(text):
    return NON_ALNUM.sub("", text.replace(SOFT, ""))


def source_alnum(page, header_texts):
    """Поток букв/цифр исходной страницы, независимо от рендера (минус колонтитулы и Dotsies)."""
    parts = []
    skip = {alnum(h) for h in header_texts}
    for b in page.get_text("dict", flags=fitz.TEXT_PRESERVE_WHITESPACE)["blocks"]:
        if b["type"] != 0:
            continue
        blk = "".join("".join(s["text"] for s in l["spans"]
                              if not s["font"].startswith(NOISE_FONTS)) for l in b["lines"])
        a = alnum(blk)
        if a in skip:
            skip.discard(a)
            continue
        parts.append(a)
    return "".join(parts)


def lost_fragments(src, got):
    sm = difflib.SequenceMatcher(None, src, got, autojunk=False)
    ops = sm.get_opcodes()
    missing = [src[a0:a1] for op, a0, a1, b0, b1 in ops if op in ("delete", "replace")]
    # фрагмент, переставленный в другое место (например, внутрь таблицы), потерей не считается
    extra = "\x00".join(got[b0:b1] for op, a0, a1, b0, b1 in ops if op in ("insert", "replace"))
    lost = []
    for frag in missing:
        k = extra.find(frag)
        if k >= 0:
            extra = extra[:k] + "\x00" + extra[k + len(frag):]
        else:
            lost.append(frag)
    return lost


# ----------------------------------------------------------------------------
def slug(s):
    s = re.sub(r"\s+", " ", s).strip()
    s = s.replace(" ", "_")
    return re.sub(r"[^\wА-Яа-яЁё_-]", "", s)


def build(cfg):
    PDF = os.path.join(ROOT, cfg["pdf"])
    OUT = os.path.join(ROOT, "Rulebook", cfg["out"])
    if not os.path.exists(PDF):
        print("Пропуск, не найден PDF: " + PDF)
        return None
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT):
        if f.endswith((".md", ".txt")):
            os.remove(os.path.join(OUT, f))

    doc = fitz.open(PDF)
    # главы = оглавление 1-го уровня по порядку страниц; записи на одной странице склеиваются
    by_page = collections.OrderedDict()
    for lvl, t, p in sorted((x for x in doc.get_toc() if x[0] == 1), key=lambda x: x[2]):
        by_page.setdefault(p - 1, []).append(re.sub(r"\s+", " ", t).strip())
    toc = [(" / ".join(ts), p) for p, ts in by_page.items()]
    chapter_titles = {t.upper() for ts in by_page.values() for t in ts}
    starts = toc if toc and toc[0][1] == 0 else [(cfg["intro"], 0)] + toc
    chapters = []
    for n, (title, s) in enumerate(starts):
        e = starts[n + 1][1] - 1 if n + 1 < len(starts) else doc.page_count - 1
        chapters.append((n, title, s, e))

    verify = io.open(os.path.join(OUT, "_verify_report.txt"), "w", encoding="utf-8")
    removed_log = io.open(os.path.join(OUT, "_removed_noise.txt"), "w", encoding="utf-8")
    empty_pages = []
    total_src = total_lost = 0
    index_rows = []

    for n, title, s, e in chapters:
        fname = "%02d_%s.md" % (n, slug(title))
        lab_s, lab_e = page_label(doc, s, cfg["cover"]), page_label(doc, e, cfg["cover"])
        index_rows.append((n, title, lab_s, lab_e, s + 1, e + 1, fname))
        with io.open(os.path.join(OUT, fname), "w", encoding="utf-8") as f:
            f.write("# %s\n\n" % title)
            f.write("_" + cfg["name"] + " — стр. %s–%s (PDF %d–%d). "
                    "Метка [стр. N | PDF M] стоит перед текстом каждой страницы._\n\n"
                    % (lab_s, lab_e, s + 1, e + 1))
            for i in range(s, e + 1):
                page = doc[i]
                label = page_label(doc, i, cfg["cover"])
                removed = []
                blocks = extract_page(page, removed)
                lines = render_page(blocks, removed, page.rect.height, chapter_titles, label,
                                    find_tables(page))
                f.write("\n---\n[стр. %s | PDF %d]\n\n" % (label, i + 1))
                if not lines:
                    f.write("_(на странице нет текста — только иллюстрации)_\n")
                    empty_pages.append((label, i + 1))
                else:
                    f.write("\n\n".join(lines) + "\n")
                for kind, t in removed:
                    removed_log.write("стр. %s | PDF %d | %s | %s\n" % (label, i + 1, kind, t.strip()))

                # проверка: буквы/цифры исходника (минус залогированный мусор) есть в результате
                src = source_alnum(page, [t for k, t in removed if k == "header"])
                lost = lost_fragments(src, alnum("\n".join(lines)))
                total_src += len(src)
                total_lost += sum(len(x) for x in lost)
                if lost:
                    verify.write("стр. %s (PDF %d): не найдено в выходе %r\n" % (label, i + 1, lost))

    verify.write("\nИТОГО букв/цифр в исходнике: %d, потеряно: %d\n" % (total_src, total_lost))
    verify.close()
    removed_log.close()

    with io.open(os.path.join(OUT, "00_INDEX.md"), "w", encoding="utf-8") as f:
        f.write("# %s — текстовая версия\n\n" % cfg["name"])
        f.write("Источник: `%s` (%d стр.).\n\n" % (cfg["pdf"], doc.page_count))
        f.write("**Как ссылаться на оригинал:** перед каждой страницей стоит метка "
                "`[стр. N | PDF M]`. N — номер, напечатанный на странице книги (по нему работают "
                "внутренние ссылки корника «см. стр. 104»), M — номер страницы в просмотрщике PDF "
                "(N+1). Номера страниц русского и английского изданий совпадают только в начале "
                "книги, дальше разъезжаются — соответствие глав в `../CHAPTERS.md`.\n\n")
        f.write("Убрано: иллюстрации, колонтитулы, декоративная латиница (полный список убранного "
                "текста — `_removed_noise.txt`). Переносы и строки абзацев склеены, таблицы — markdown-"
                "таблицы (если таблицу не удалось распознать без потерь — строки с ` | ` между ячейками), "
                "врезки на полях помечены `>`.\n\n")
        f.write("| # | Глава | Стр. книги | PDF | Файл |\n|---|---|---|---|---|\n")
        for n, title, ls, le, ps, pe, fname in index_rows:
            f.write("| %d | %s | %s–%s | %d–%d | [%s](%s) |\n" % (n, title, ls, le, ps, pe, fname, fname))
        f.write("\nСтраницы без текста (только иллюстрации): " +
                ", ".join("%s (PDF %d)" % x for x in empty_pages) + "\n")

    print("[%s] готово: %s" % (cfg["out"], OUT))
    print("[%s] букв/цифр в исходнике: %d, потеряно: %d" % (cfg["out"], total_src, total_lost))
    return [(title, ls, le, cfg["out"] + "/" + fname) for n, title, ls, le, ps, pe, fname in index_rows]


# разделы английского PDF, которых нет отдельными главами в оглавлении русского
# ("Your Character Sheet" в RU входит в главу «Внедряем кибер в панк»)
EN_FRONT_BACK = {"Front Cover", "Title Page", "Contents", "Advertisements", "Your Character Sheet",
                 "Character Sheet", "Index", "Back Cover"}


def write_chapter_map(ru, en):
    """Соответствие глав RU <-> EN: состав и порядок основных глав в изданиях одинаковый."""
    ru_main = ru[1:]  # без вступления
    en_main = [r for r in en if not set(r[0].split(" / ")) <= EN_FRONT_BACK]
    with io.open(os.path.join(ROOT, "Rulebook", "CHAPTERS.md"), "w", encoding="utf-8") as f:
        f.write("# Соответствие глав: русский перевод v1.3 ↔ английский оригинал\n\n")
        f.write("Страницы — печатные номера книги. Используй, чтобы найти то же место в другом издании.\n\n")
        if len(ru_main) == len(en_main):
            f.write("| RU глава | RU стр. | EN chapter | EN pages |\n|---|---|---|---|\n")
            for (rt, ra, rb, rf), (et, ea, eb, ef) in zip(ru_main, en_main):
                f.write("| [%s](%s) | %s–%s | [%s](%s) | %s–%s |\n" % (rt, rf, ra, rb, et, ef, ea, eb))
        else:
            f.write("_Число глав не совпало (%d vs %d) — списки раздельно._\n\n" % (len(ru_main), len(en_main)))
            for name, rows in (("RU", ru), ("EN", en)):
                f.write("\n## %s\n\n" % name)
                for t, a, b, fn in rows:
                    f.write("- [%s](%s) — %s–%s\n" % (t, fn, a, b))


def main():
    keys = [a for a in sys.argv[1:] if a in BOOKS] or list(BOOKS)
    results = {k: build(BOOKS[k]) for k in keys}
    # карта глав пересобирается, только когда в этом запуске собраны обе книги
    if results.get("ru") and results.get("en"):
        write_chapter_map(results["ru"], results["en"])


if __name__ == "__main__":
    main()
