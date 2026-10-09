# Cyberpunk RED Terminal 2.0.0

## Русский

Большое обновление: новая рабочая область, два детальных стиля листа, материалы DLC, карта Найт-Сити и английский интерфейс.

**Интерфейс**
- Боковая панель и рабочая область: все окна листа (настройки, финансы и жильё, прокачка, экспорт и печать, каталоги, нетраннинг) открываются вкладками, а не отдельными окнами. Старая раскладка (вкладки сверху и выдвижное меню) осталась на выбор в настройках.
- Стиль листа в настройках: минималистичный и два детальных. В детальных есть шапка с портретом и шкалами ПЗ, брони и человечности, карточки, полоски уровней навыков и характеристик, иконки категорий. Второй детальный стиль — «неон» с акцентными контурами.
- Шрифт Bahnschrift по умолчанию (старый Consolas один раз переносится на него; выбранный вручную шрифт сохраняется). Девять цветовых тем, в том числе светлая.
- Вкачанные навыки подсвечиваются целой строкой.
- Английский интерфейс: настройка «Язык / Language», действует после перезапуска.

**Игра**
- Материалы DLC: предметы, оружие, броня, транспорт, жильё, деки и железо для нетраннера, НИПы, Чёрный хром, таро, погода, агенты, штаб-квартира, сбор обломков, панкнаут, ночные рынки, «Прожжённые» перчинки.
- «Переходи на Металл»: полная конверсия в борга по 13 корпусам (ТЕЛ, ПЗ и человечность по эндоскелету и набору имплантов, потеря человечности кубом или средним).
- Экономика: образ жизни, жильё, ежемесячный счёт во вкладке «Финансы и жильё».
- Карта Найт-Сити: районы, места, недвижимость и рынки, слои, масштаб и свои метки.
- Журнал сессии: заметки, портрет, броски навыков по клику.
- «Правила (выдержки)» одной страницей; правый клик по навыку открывает нужный блок.
- Терминал ГМа: сохранение боя, генератор столкновений, нумерация копий врагов, состояния и таймеры.

**Надёжность**
- Схема сохранений с миграциями; перед миграцией делается копия. Старые сохранения (0.5–1.9) открываются.
- Журнал ошибок, версия в заголовках окон, проверка обновлений на GitHub.
- 268 автотестов.

**Исправления**
- После ввода ОУ роль можно прокачать сразу, без лишней покупки.
- Повторное открытие уже открытой вкладки больше не оставляет «висящих» подписок и не теряет сохранение прокачки.
- Убраны кнопки «+/−» у роли в шапке; удалён неиспользуемый код добавления и удаления роли (новая роль берётся в «Улучшении»).

**Известные ограничения**
- Тексты правил, предметы DLC и имена НИПов остаются на русском и в английском интерфейсе; тексты системных окон-сообщений не переведены.
- Экспорт в PDF в классической раскладке с открытыми вкладками инструментов может уменьшить страницу.

---

## English

A major update: a new workspace, two detailed sheet styles, DLC content, the Night City map and an English interface.

**Interface**
- Side panel and workspace: every sheet window (settings, finance and housing, improvement, export and print, catalogs, netrunning) now opens as a tab instead of a separate window. The old layout (tabs on top and a slide-out menu) is still available in settings.
- Sheet style setting: minimalist plus two detailed styles. The detailed ones add a header with a portrait and HP, armor and humanity gauges, cards, level bars for skills and stats, and category icons. The second detailed style is "neon" with accent outlines.
- Bahnschrift is the default font (the old Consolas default is migrated once; a font you picked yourself is kept). Nine color themes, including a light one.
- Raised skills are highlighted across the whole row.
- English interface: the "Язык / Language" setting, applied after a restart.

**Gameplay**
- DLC content: items, weapons, armor, vehicles, housing, decks and hardware for netrunners, NPCs, Black Chrome, tarot, weather, agents, headquarters, salvage, punknaut, night markets, the "Burnt" twists.
- "Go Metal": full conversion to a borg across 13 chassis (BODY, HP and humanity from the endoskeleton and the implant kit, humanity loss by dice roll or average).
- Economics: lifestyle, housing and a monthly bill in the "Finance and housing" tab.
- Night City map: districts, places, properties and markets, layers, zoom and your own pins.
- Session journal: notes, a portrait, and skill rolls by click.
- "Rules (excerpts)" on one page; right-clicking a skill opens the matching block.
- GM terminal: combat save, encounter generator, numbered enemy copies, states and timers.

**Reliability**
- A save schema with migrations; a copy is made before migrating. Old saves (0.5 to 1.9) still open.
- Error log, version in window titles, update check against GitHub.
- 268 automated tests.

**Fixes**
- After typing the improvement points (IP) you can upgrade a role immediately, with no extra purchase.
- Reopening an already open tab no longer leaves dangling subscriptions or loses the improvement save.
- Removed the "+/-" buttons from the role header and the unused add/remove role code (a new role is taken in "Improvement").

**Known limitations**
- Rule texts, DLC items and NPC names stay in Russian in the English interface; system message boxes are not translated.
- PDF export in the classic layout with tool tabs open can shrink the page.
