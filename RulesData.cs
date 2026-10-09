using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CyberpunkRED_Generator
{
    public enum RuleKind { Para, Formula, Bullets, Table, Note }

    public class RuleItem
    {
        public RuleKind Kind;
        public string Ru, En;                  // абзац, формула или заметка
        public string[] BulletsRu, BulletsEn;  // список
        public string[] Head;                  // таблица: заголовки и строки, ячейка «рус|eng» (без «|» — одинакова в обоих языках)
        public List<string[]> Rows;
    }

    public class RuleBlock
    {
        public string Id, TitleRu, TitleEn, Pages, Keys;
        public List<RuleItem> Items = new List<RuleItem>();

        public string Title => Loc.English ? TitleEn : TitleRu;
        public string Source => Loc.English ? "Core Rulebook, pg. " + Rules.Pick(Pages, true) : "Основная книга, стр. " + Rules.Pick(Pages, false);
    }

    // «Правила»: выдержки из основной книги Cyberpunk RED v1.3 одной длинной страницей из блоков.
    // Тексты сверены по русскому (Rulebook/ru) и английскому (Rulebook/en) изданиям. Где русский текст расходится с оригиналом
    // (опечатки перевода), взято значение оригинала; список расхождений — в конце файла.
    public static class Rules
    {
        public static readonly List<RuleBlock> Blocks = Build();

        // ячейка «рус|eng» → строка на языке интерфейса; {50} → «50eb (Недёшево)»
        public static string Pick(string cell, bool english) => Expand(Split(cell, english), english);

        private static string Split(string cell, bool english)
        {
            if (cell == null) return "";
            int i = cell.IndexOf('|');
            return i < 0 ? cell : (english ? cell.Substring(i + 1) : cell.Substring(0, i));
        }

        // {50} → «50eb (Недёшево)»
        public static string Expand(string s, bool english)
            => Regex.Replace(s ?? "", @"\{(\d+)\}", m => Price(int.Parse(m.Groups[1].Value), english));

        public static string Price(int eb, bool english)
        {
            string ru, en;
            switch (eb)
            {
                case 10: ru = "Дёшево"; en = "Cheap"; break;
                case 20: ru = "Повседневно"; en = "Everyday"; break;
                case 50: ru = "Недёшево"; en = "Costly"; break;
                case 100: ru = "Премиум"; en = "Premium"; break;
                case 500: ru = "Дорого"; en = "Expensive"; break;
                case 1000: ru = "Очень дорого"; en = "Very Expensive"; break;
                case 5000: ru = "Роскошь"; en = "Luxury"; break;
                case 10000: ru = "Супер роскошь"; en = "Super Luxury"; break;
                default: return eb.ToString("N0") + "eb";
            }
            return eb.ToString("N0") + "eb (" + (english ? en : ru) + ")";
        }

        // весь текст блока на обоих языках (для поиска)
        public static string SearchText(RuleBlock b)
        {
            var sb = new StringBuilder();
            sb.Append(b.TitleRu).Append(' ').Append(b.TitleEn).Append(' ').Append(b.Keys).Append(' ');
            foreach (var it in b.Items)
            {
                sb.Append(it.Ru).Append(' ').Append(it.En).Append(' ');
                if (it.BulletsRu != null) sb.Append(string.Join(" ", it.BulletsRu)).Append(' ').Append(string.Join(" ", it.BulletsEn)).Append(' ');
                if (it.Head != null)
                {
                    foreach (var h in it.Head) sb.Append(Pick(h, false)).Append(' ').Append(Pick(h, true)).Append(' ');
                    foreach (var r in it.Rows) foreach (var c in r) sb.Append(Pick(c, false)).Append(' ').Append(Pick(c, true)).Append(' ');
                }
            }
            return sb.ToString().ToLowerInvariant();
        }

        // блок, который лучше всего отвечает запросу: по названию и ключевым словам, затем по тексту
        public static RuleBlock Find(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            string q = query.Trim().ToLowerInvariant();
            var byKey = Blocks.FirstOrDefault(b => (b.TitleRu + " " + b.TitleEn + " " + b.Keys).ToLowerInvariant().Contains(q));
            return byKey ?? Blocks.FirstOrDefault(b => SearchText(b).Contains(q));
        }

        // слова запроса должны встретиться в блоке все (в любом порядке)
        public static bool Matches(RuleBlock b, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            string text = SearchText(b);
            return query.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).All(w => text.Contains(w));
        }

        // ---- построение
        private static RuleItem P(string ru, string en) => new RuleItem { Kind = RuleKind.Para, Ru = ru, En = en };
        private static RuleItem F(string ru, string en) => new RuleItem { Kind = RuleKind.Formula, Ru = ru, En = en };
        private static RuleItem N(string ru, string en) => new RuleItem { Kind = RuleKind.Note, Ru = ru, En = en };
        private static RuleItem L(string[] ru, string[] en) => new RuleItem { Kind = RuleKind.Bullets, BulletsRu = ru, BulletsEn = en };
        private static RuleItem T(string[] head, params string[][] rows) => new RuleItem { Kind = RuleKind.Table, Head = head, Rows = rows.ToList() };
        private static string[] H(params string[] c) => c;
        private static string[] R(params string[] c) => c;
        private static string[] S(params string[] c) => c;

        private static RuleBlock B(string id, string ru, string en, string pages, string keys, params RuleItem[] items)
            => new RuleBlock { Id = id, TitleRu = ru, TitleEn = en, Pages = pages, Keys = keys, Items = items.ToList() };

        private static List<RuleBlock> Build()
        {
            return new List<RuleBlock>
            {
                // ------------------------------------------------------------------------------------------------
                B("stats", "ПРОИЗВОДНЫЕ ХАРАКТЕРИСТИКИ", "DERIVED STATS", "79–80",
                    "пз hp хп здоровье пункты здоровья человечность эмп эмпатия сила воли телосложение humanity empathy hit points",
                    F("ПЗ = 10 + 5 × ((ТЕЛО + ВОЛЯ) ÷ 2, округлить вверх)", "HP = 10 + 5 × ((BODY + WILL) ÷ 2, round up)"),
                    P("Порог тяжёлого ранения — половина максимальных ПЗ (вверх). Спасбросок от смерти равен ТЕЛО.", "The Seriously Wounded threshold is half of your maximum HP (rounded up). Your Death Save equals your BODY."),
                    P("Человечность = ЭМП × 10 (ЭМП 5 → 50 человечности). Когда меняется десяток человечности, ЭМП падает до этого десятка: при 44 ЭМП 4, при 39 уже ЭМП 3.", "Humanity = EMP × 10 (EMP 5 → 50 Humanity). Whenever the tens place of your Humanity drops, EMP drops to match: 44 Humanity is EMP 4, 39 is EMP 3."),
                    T(H("ТЕЛО \\ ВОЛЯ|BODY \\ WILL", "2", "4", "6", "8", "10", "12", "14"),
                        R("2", "20", "25", "30", "35", "40", "45", "50"),
                        R("4", "25", "30", "35", "40", "45", "50", "55"),
                        R("6", "30", "35", "40", "45", "50", "55", "60"),
                        R("8", "35", "40", "45", "50", "55", "60", "65"),
                        R("10", "40", "45", "50", "55", "60", "65", "70"))),

                // ------------------------------------------------------------------------------------------------
                B("check", "ПРОВЕРКА НАВЫКА", "SKILL CHECKS", "129–131",
                    "проверка навык сложность сл dv кубик d10 критический успех провал модификатор удача смежные повтор характеристика интеллект ловкость техника харизма концентрация",
                    F("ХАР + навык + 1d10  против  СЛ", "STAT + Skill + 1d10  vs  DV"),
                    P("Нужно превзойти СЛ. При ничьей побеждает защищающийся. Если навыка нет, берётся одна ХАР + 1d10. Простое, очевидное действие бросков не требует.", "You must beat the DV. In a tie the Defender wins. With no Skill you roll only the linked STAT + 1d10. Obvious, simple actions need no roll."),
                    P("Встречная проверка: нападающий бросает ХАР + навык + 1d10 против ХАР + навык + 1d10 цели; результат цели и есть СЛ.", "Opposed Check: the attacker rolls STAT + Skill + 1d10 against the defender's own STAT + Skill + 1d10, and that result is the DV."),
                    T(H("Сложность|Difficulty", "СЛ|DV", "Что это|What it means"),
                        R("Просто|Simple", "9", "Большинство людей справятся не задумываясь.|Most people can do it without thinking."),
                        R("Буднично|Everyday", "13", "Не требует серьёзной подготовки.|Needs no special training."),
                        R("Трудно|Difficult", "15", "Трудно без обучения или таланта.|Hard without training or talent."),
                        R("Профессионально|Professional", "17", "Нужна реальная подготовка.|Takes real training."),
                        R("Героически|Heroic", "21", "Только лучшие из лучших.|Only the best of the best."),
                        R("Невероятно|Incredible", "24", "Поразительное достижение.|A tremendous feat."),
                        R("Легендарно|Legendary", "29", "О таком пишут истории.|Something people write stories about.")),
                    P("Критический успех: на кубе выпала 10 — брось ещё 1d10 и прибавь (вторая 10 ничего не даёт). Критический провал: выпала 1 — брось ещё 1d10 и вычти (вторая 1 ничего не даёт).", "Critical Success: a natural 10 — roll another 1d10 and add it (a second 10 adds nothing more). Critical Failure: a natural 1 — roll another 1d10 and subtract it (a second 1 does nothing more)."),
                    T(H("Условие|Condition", "Модификатор|Mod"),
                        R("Темно или плохое освещение|Night or low lighting", "−1"),
                        R("Раньше этого не делал|Never done this before", "−1"),
                        R("Сложная задача|Complex task", "−2"),
                        R("Нет нужных инструментов или деталей|No right tools or parts", "−2"),
                        R("Плохо спал|Slept uncomfortably", "−2"),
                        R("Сильный стресс|Under extreme stress", "−2"),
                        R("Измотан|Exhausted", "−4"),
                        R("Сильно пьян или под седативными|Extremely drunk or sedated", "−4"),
                        R("Пытаешься делать тайно|Trying to act secretly", "−4"),
                        R("Задымление, тьма|Obscured by smoke or darkness", "−4")),
                    N("Модификаторы суммируются и вычитаются из результата. Повторная попытка после провала возможна, только если шансы выросли: больше времени, лучший инструмент или смежный навык.", "Modifiers are cumulative and are subtracted from the roll. After a failure you may only try again if your chances improved: more time, a better tool or a Complementary Skill."),
                    L(new[]
                    {
                        "Смежные навыки: удачный бросок по связанному навыку (даже чужой) даёт +1 к следующей попытке, один раз, бонусы не складываются.",
                        "Дополнительное время: в 4 раза больше времени — единственный бонус +1.",
                        "Удача: до броска потрать очки из запаса УДЧ, каждое даёт +1 к броску. Запас равен УДЧ и полностью восстанавливается в начале сессии."
                    }, new[]
                    {
                        "Complementary Skills: a good roll in a related Skill (even by another character) gives +1 to the next attempt, once; bonuses do not stack.",
                        "Taking Extra Time: spending four times as long gives a single +1.",
                        "LUCK: before you roll, spend points from your LUCK Pool, +1 to the roll each. The Pool equals your LUCK and refills fully each session."
                    })),

                // ------------------------------------------------------------------------------------------------
                B("turn", "БОЙ: ХОД, ДЕЙСТВИЯ, ДВИЖЕНИЕ", "COMBAT: TURNS, ACTIONS, MOVEMENT", "126–128, 168–170",
                    "бой ход раунд инициатива реакция скорость действие перемещение бег лёжа подъём скор прицельная атака голова нога перезарядка отложенное initiative action move run prone aimed shot",
                    F("Инициатива = РЕФ + 1d10", "Initiative = REF + 1d10"),
                    P("Ход длится около 3 секунд; круг ходов всех участников — раунд (тоже около 3 секунд). Очередь идёт от большего значения к меньшему, при ничьей перебрасывают. Каждый ход: 1 действие перемещения + 1 действие.", "A Turn lasts about 3 seconds; a full cycle of everyone's Turns is a Round (also about 3 seconds). The Initiative Queue runs highest to lowest; ties are re-rolled. Every Turn: 1 Move Action + 1 Action."),
                    P("Действие перемещения: до СКО × 2 метров за ход (по сетке — СКО клеток, диагональ разрешена; 1 клетка = 2 м). Дальность касания — 2 м. Плавание, лазание и прыжок с разбега стоят 2 м за 1 м; прыжок с места — половина дистанции.", "Move Action: up to MOVE × 2 m/yds per Turn (on a grid, MOVE squares, diagonals allowed; 1 square = 2 m/yds). Reach is 2 m/yds. Swimming, climbing and running jumps cost 2 m per 1 m; a standing jump covers half the distance."),
                    T(H("Действие|Action", "Что делает|What it does"),
                        R("Атака|Attack", "Атака в рукопашном или дальнем бою.|A Melee or Ranged Attack."),
                        R("Бег|Run", "Ещё одно действие перемещения, только если уже двигался в этом ходу.|An extra Move Action, only if you already moved this Turn."),
                        R("Подъём|Get Up", "Встать из положения лёжа. Пока лежишь, действие перемещения недоступно.|Stand up from Prone. While Prone you cannot use a Move Action."),
                        R("Перезарядка|Reload", "Полная замена магазина одним типом боеприпасов.|Fully reload with a single ammunition type."),
                        R("Захват, бросок, удушение|Grab, Throw, Choke", "См. блок «Рукопашный бой».|See the Melee block."),
                        R("Отложенное действие|Hold Action", "Выбери событие или номер в очереди; на несколько раундов откладывать нельзя, передумать нельзя, перемещение и сетевые действия не откладываются.|Pick a trigger or queue number; cannot be held across Rounds or changed; Move and NET Actions can't be held."),
                        R("Использование объекта|Use an Object", "Всё, что не требует навыка и не описано отдельно (дверь, детонатор, подобрать оружие). Достать лёгкодоступное оружие в свободную руку и уронить оружие — не действие; убрать его — действие.|Anything that needs no Skill (open a door, detonator, pick up a weapon). Drawing an easy weapon into a free hand and dropping a weapon are free; stowing is an Action."),
                        R("Использование навыка|Use a Skill", "Быстрая задача до 3 секунд. Дольше — тратишь действие каждый ход и бросаешь, когда время оплачено.|A quick task up to 3 seconds. Longer tasks take an Action each Turn and you roll once the full time is paid."),
                        R("Стабилизация|Stabilize", "Стартовать естественное лечение или вытащить из смертельного ранения.|Begin natural healing or pull a target out of Mortally Wounded."),
                        R("Сетевые действия|NET Actions", "Только нетраннеры, см. блок «Нетраннинг».|Netrunners only, see the Netrunning block."),
                        R("Транспорт|Vehicles", "Сесть, завести, остановить, манёвр — каждое действие. Выйти — просто перемещение.|Get in, start, stop, Maneuver are Actions. Getting out is just movement.")),
                    L(new[]
                    {
                        "Разделение: действие можно потратить посреди перемещения и продолжить движение. Атаки со СКОР 2 можно разделять между перемещениями.",
                        "Не больше двух проверок атаки за одно действие. Двумя видами оружия со СКОР 1 в одном действии не атакуют.",
                        "Лёжа: пока не потратишь действие «подъём», двигаться нельзя."
                    }, new[]
                    {
                        "Splitting: take your Action in the middle of a Move Action and keep moving. Attacks from ROF 2 weapons can be split across the Move.",
                        "No more than two Attack Checks in one Action. You cannot attack with two ROF 1 weapons in the same Action.",
                        "Prone: you cannot move until you use the Get Up Action."
                    }),
                    T(H("Прицельная атака (−8, всё действие, СКОР 1)|Aimed Shot (−8, whole Action, ROF 1)", "Эффект при попадании|Effect on hit"),
                        R("Голова|Head", "Урон, прошедший через броню головы, × 2.|Damage that gets through head armor × 2."),
                        R("Предмет в руках|Held Item", "Если прошёл хотя бы 1 урон через броню тела — цель роняет один предмет на твой выбор.|If at least 1 damage gets through body armor, the target drops one held item of your choice."),
                        R("Нога|Leg", "Если прошёл хотя бы 1 урон через броню тела — перелом ноги, если есть нога без перелома.|If at least 1 damage gets through body armor, Broken Leg (if a leg is still unbroken).")),
                    N("Прицельную атаку нельзя совмещать с автоогнём, дробью и специальными приёмами боевых искусств (кроме оговорённых).", "Aimed Shots can't be combined with Autofire, Shotgun Shells or Martial Arts Special Moves (unless stated).")),

                // ------------------------------------------------------------------------------------------------
                B("ranged", "ДАЛЬНИЙ БОЙ", "RANGED COMBAT", "170–174",
                    "дальний бой стрельба оружие пистолет короткоствольное оружие длинноствольное оружие тяжёлое оружие луки и арбалеты дробовик винтовка автоогонь подавляющий огонь дробь взрывчатка стрелы дистанция сл ranged autofire suppressive shotgun explosive range",
                    F("РЕФ + навык оружия + 1d10  против  СЛ по дистанции (или ЛВК + уклонение + 1d10)", "REF + Weapon Skill + 1d10  vs  range DV (or DEX + Evasion + 1d10)"),
                    P("Цель с РЕФ 8 и выше может уклоняться вместо СЛ по таблице. При ничьей выигрывает защита. Броня уменьшает урон (см. блок «Броня»).", "A defender with REF 8 or higher may dodge instead of using the range table. Ties go to the defender. Armor reduces the damage (see Armor)."),
                    T(H("Оружие|Weapon", "Навык|Skill", "Урон|Dmg", "Магазин|Mag", "СКОР|ROF", "Рук|Hands", "Скрыть|Conceal", "Цена|Cost"),
                        R("Средний пистолет|Medium Pistol", "Короткоствольное|Handgun", "2d6", "12", "2", "1", "да|yes", "{50}"),
                        R("Тяжёлый пистолет|Heavy Pistol", "Короткоствольное|Handgun", "3d6", "8", "2", "1", "да|yes", "{100}"),
                        R("Очень тяжёлый пистолет|Very Heavy Pistol", "Короткоствольное|Handgun", "4d6", "8", "1", "1", "нет|no", "{100}"),
                        R("ПП|SMG", "Короткоствольное|Handgun", "2d6", "30", "1", "1", "да|yes", "{100}"),
                        R("Тяжёлый ПП|Heavy SMG", "Короткоствольное|Handgun", "3d6", "40", "1", "1", "нет|no", "{100}"),
                        R("Дробовик|Shotgun", "Длинноствольное|Shoulder Arms", "5d6", "4", "1", "2", "нет|no", "{500}"),
                        R("Штурмовая винтовка|Assault Rifle", "Длинноствольное|Shoulder Arms", "5d6", "25", "1", "2", "нет|no", "{500}"),
                        R("Снайперская винтовка|Sniper Rifle", "Длинноствольное|Shoulder Arms", "5d6", "4", "1", "2", "нет|no", "{500}"),
                        R("Лук, арбалет|Bow, Crossbow", "Луки и арбалеты|Archery", "4d6", "—", "1", "2", "нет|no", "{100}"),
                        R("Гранатомёт|Grenade Launcher", "Тяжёлое оружие|Heavy Weapons", "6d6", "2", "1", "2", "нет|no", "{500}"),
                        R("Ракетница|Rocket Launcher", "Тяжёлое оружие|Heavy Weapons", "8d6", "1", "1", "2", "нет|no", "{500}")),
                    T(H("СЛ одиночного выстрела|Single Shot DV", "0–6 м|0–6 m", "7–12", "13–25", "26–50", "51–100", "101–200", "201–400", "401–800"),
                        R("Пистолет|Pistol", "13", "15", "20", "25", "30", "30", "—", "—"),
                        R("ПП|SMG", "15", "13", "15", "20", "25", "25", "30", "—"),
                        R("Дробовик (жакан)|Shotgun (Slug)", "13", "15", "20", "25", "30", "35", "—", "—"),
                        R("Штурмовая винтовка|Assault Rifle", "17", "16", "15", "13", "15", "20", "25", "30"),
                        R("Снайперская винтовка|Sniper Rifle", "30", "25", "25", "20", "15", "16", "17", "20"),
                        R("Лук, арбалет|Bow, Crossbow", "15", "13", "15", "17", "20", "22", "—", "—"),
                        R("Гранатомёт|Grenade Launcher", "16", "15", "15", "17", "20", "22", "25", "—"),
                        R("Ракетница|Rocket Launcher", "17", "16", "15", "15", "20", "20", "25", "30")),
                    P("Автоогонь: действие и 10 патронов (меньше нельзя). Навык автоогонь вместо навыка оружия, СЛ по таблице автоогня, прицельно стрелять нельзя, цели с РЕФ 8+ могут уклоняться. При попадании брось 2d6 и умножь на то, на сколько ты превзошёл СЛ, но не больше предела оружия (ПП — 3, штурмовая винтовка — 4). Если на обоих кубах 6 — критическая травма.", "Autofire: an Action and 10 bullets (fewer and you can't). Use the Autofire Skill instead of the Weapon Skill and the Autofire table; no Aimed Shots; targets with REF 8+ can dodge. On a hit roll 2d6 and multiply by the amount you beat the DV, up to the weapon's cap (SMG 3, Assault Rifle 4). If both dice show 6 it is a Critical Injury."),
                    T(H("СЛ автоогня|Autofire DV", "0–6", "7–12", "13–25", "26–50", "51–100"),
                        R("ПП|SMG", "20", "17", "20", "25", "30"),
                        R("Штурмовая винтовка|Assault Rifle", "22", "20", "17", "20", "25")),
                    L(new[]
                    {
                        "Подавляющий огонь: действие и 10 патронов. Все пешие цели в 25 м, вне укрытия и в линии видимости, бросают ВОЛЯ + концентрация + 1d10 против твоих РЕФ + автоогонь + 1d10. Провалившие обязаны своим следующим перемещением уйти в укрытие (при нехватке — ещё и «бег»).",
                        "Дробь: нельзя целиться. Атака РЕФ + длинноствольное оружие + 1d10 против СЛ 13; при успехе все цели перед тобой в 6 м (3 клетки) получают 3d6 (урон бросается один раз). Цели с РЕФ 8+ могут уклоняться.",
                        "Взрывчатка: урон один раз всем в области 10 × 10 м (5 × 5 клеток) вокруг цели 2 × 2 м. Промах — рефери сам решает, куда упал взрыв в этой области. РЕФ 8+ может уклониться, выбросив больше твоей проверки. За укрытием урон не проходит, если не разрушает укрытие.",
                        "Стрелы: заряжание входит в атаку, перезарядка не нужна; обычные стрелы можно подобрать."
                    }, new[]
                    {
                        "Suppressive Fire: an Action and 10 bullets. Everyone on foot within 25 m/yds, out of cover and in line of sight rolls WILL + Concentration + 1d10 against your REF + Autofire + 1d10. Failures must use their next Move Action to reach cover (and Run if needed).",
                        "Shotgun Shell: no Aimed Shots. Attack REF + Shoulder Arms + 1d10 vs DV13; on a hit every target in front of you within 6 m/yds (3 squares) takes 3d6 (rolled once). Targets with REF 8+ may dodge.",
                        "Explosives: damage rolled once for everyone in a 10 × 10 m/yd (5 × 5 square) area around a 2 × 2 m target square. On a miss the GM decides where in that area it lands. REF 8+ may dodge by beating your Check. Cover absorbs the blast unless the damage destroys it.",
                        "Arrows: loading is part of the attack, no Reload needed; basic arrows can be retrieved."
                    })),

                // ------------------------------------------------------------------------------------------------
                B("melee", "РУКОПАШНЫЙ БОЙ И БОЕВЫЕ ИСКУССТВА", "MELEE AND MARTIAL ARTS", "175–180",
                    "рукопашный бой холодное оружие драка захват удушение бросок боевые искусства уклонение каратэ дзюдо айкидо тхэквондо melee brawling grapple choke throw martial arts evasion",
                    F("ЛВК + навык ближнего боя + 1d10  против  ЛВК + уклонение + 1d10", "DEX + Melee skill + 1d10  vs  DEX + Evasion + 1d10"),
                    P("Цель должна быть в досягаемости (2 м). Весь ближний бой со СКОР 2, кроме очень тяжёлого холодного оружия (СКОР 1). Холодное оружие и боевые искусства игнорируют половину брони (вверх); драка — нет. Оружие держат нужным числом рук; с ТЕЛО 8+ двуручное можно держать одной рукой.", "The target must be in reach (2 m/yds). All melee is ROF 2 except Very Heavy Melee Weapons (ROF 1). Melee weapons and Martial Arts ignore half of armor (round up); Brawling does not. Hold weapons in the hands they were made for; with BODY 8+ a two-handed weapon can be held in one."),
                    T(H("Холодное оружие|Melee Weapon", "Примеры|Examples", "Урон|Dmg", "СКОР|ROF", "Скрыть|Conceal", "Цена|Cost"),
                        R("Лёгкое|Light", "боевой нож, томагавк|combat knife, tomahawk", "1d6", "2", "да|yes", "{50}"),
                        R("Среднее|Medium", "бита, лом, мачете|baseball bat, crowbar, machete", "2d6", "2", "нет|no", "{50}"),
                        R("Тяжёлое|Heavy", "свинцовая труба, меч, шипованная бита|lead pipe, sword, spiked bat", "3d6", "2", "нет|no", "{100}"),
                        R("Очень тяжёлое|Very Heavy", "бензопила, кувалда, нагината|chainsaw, sledgehammer, naginata", "4d6", "1", "нет|no", "{500}")),
                    T(H("Урон драки и боевых искусств по ТЕЛО|Brawling and Martial Arts damage by BODY", "≤ 4", "5–6", "7–10", "11+"),
                        R("Урон|Damage", "1d6", "2d6", "3d6", "4d6")),
                    N("С киберрукой урон драки не ниже 2d6 (ТЕЛО ≤ 4 считается как 5–6).", "With a Cyberarm, Brawling damage is at least 2d6 (BODY ≤ 4 counts as 5–6)."),
                    L(new[]
                    {
                        "Захват (действие): ты и цель бросаете ЛВК + драка + 1d10. Выиграл — схватил цель или вырвал один предмет из её рук. Схваченные в борьбе получают −2 ко всем действиям, цель не двигается и тащится за тобой, двуручное оружие использовать нельзя. Нападающий отпускает без действия; чтобы вырваться, нужен выигранный захват.",
                        "Удушение (действие): урон равен ТЕЛО напрямую по ПЗ, броню игнорирует. Не убивает: с более чем 1 ПЗ остаётся 1 ПЗ и без сознания. Три раунда подряд — без сознания в любом случае.",
                        "Бросок (действие): цель на землю, урон равен ТЕЛО напрямую по ПЗ, борьба кончается, цель лёжа. Предмет: ЛВК + атлетика + 1d10, до 25 м, СЛ как у гранатомёта; метательное оружие урон обычный, но броню не режет пополам.",
                        "Боевые искусства: нужен хотя бы 1 пункт в навыке, СКОР 2, приёмы работают только при пункте в своей форме, прицельно приёмы не применяют (кроме оговорённых).",
                        "Общий приём «Восстановление»: при «подъёме» бросок СЛ 13 — и подъём не тратит действие."
                    }, new[]
                    {
                        "Grab (Action): you and the target roll DEX + Brawling + 1d10. Win: grab the target or take one held object. Grappled characters get −2 to all Actions; the target can't Move and is dragged along, no two-handed weapons. The attacker can let go for free; escaping takes a winning Grab.",
                        "Choke (Action): damage equal to BODY straight to HP, ignores armor. Won't kill: from more than 1 HP they stay at 1 HP and Unconscious. Three Rounds in a row — Unconscious regardless.",
                        "Throw (Action): target to the ground, BODY damage direct to HP, Grapple ends, target Prone. Object: DEX + Athletics + 1d10, up to 25 m/yds, Grenade Launcher DV; thrown melee weapons deal normal damage but don't halve SP.",
                        "Martial Arts: need at least 1 point in the Skill, ROF 2; Special Moves need a point in their Form; no Aimed Shots with Special Moves unless stated.",
                        "Shared move «Recovery»: on Get Up roll DV13 — success means Get Up costs no Action."
                    }),
                    T(H("Форма|Form", "Приём|Move", "Требование|Requirement", "СЛ|DV", "Эффект|Effect"),
                        R("Айкидо|Aikido", "Обезоруживающая комбинация|Disarming Combination", "попал дракой и боевыми искусствами по одной цели|hit one target with Brawling and Martial Arts", "15", "предмет из рук цели оказывается у тебя или на полу|held object ends up in your hand or on the floor"),
                        R("Айкидо|Aikido", "Железная хватка|Iron Grip", "цель схвачена|target Grabbed", "15", "−2 к попыткам вырваться, цель не стреляет|−2 to escape attempts, no Ranged Attacks"),
                        R("Каратэ|Karate", "Пролом брони|Armor Breaking Combination", "попал оружием и боевыми искусствами по одной цели|hit one target with a Melee Weapon and Martial Arts", "15", "вся броня цели ломается ещё на 2|all worn armor ablated by 2 more"),
                        R("Каратэ|Karate", "Удар, ломающий кости|Bone Breaking Strike", "ВОЛЯ 8+|WILL 8+", "—", "вместо двух атак: сломанные рёбра; с −8 в голову — трещина черепа|instead of two attacks: Broken Ribs; with −8 to the head, Cracked Skull"),
                        R("Дзюдо|Judo", "Контрбросок|Counter Throw", "уклонился от всех атак с прошлого хода|dodged all Melee Attacks since your last Turn", "15", "бросок нападавшего без захвата|Throw one attacker without grappling"),
                        R("Дзюдо|Judo", "Вырваться из захвата|Grab Escape", "дважды попал по держащему тебя|hit your grappler twice", "15", "свободен, у цели сломана рука на твой выбор|you are free, target gets Broken Arm of your choice"),
                        R("Тхэквондо|Taekwondo", "Удар по болевым точкам|Pressure Point Strike", "ВОЛЯ 8+|WILL 8+", "—", "вместо двух атак: травма позвоночника; с −8 в голову — травма мозга|instead of two attacks: Spinal Injury; with −8 to the head, Brain Injury"),
                        R("Тхэквондо|Taekwondo", "Летящий удар ногой|Flying Kick", "СКО 8+, уже прошёл 4+ м|MOVE 8+, moved 4+ m", "—", "урон по телу, цель лёжа и сброшена с открытого транспорта|damage to the body, target Prone and thrown off open vehicles"))),

                // ------------------------------------------------------------------------------------------------
                B("armor", "БРОНЯ, УКРЫТИЯ И ЩИТЫ", "ARMOR, COVER AND SHIELDS", "182–186",
                    "броня ос стопинг пауэр укрытие щит живой щит абляция штраф sp armor cover shield human shield ablate",
                    P("Урон: 1) атакующий бросает урон; 2) вычти ОС брони в зоне (тело, если не целились в голову), остаток вычти из ПЗ; 3) если получен хоть 1 урон — броня в этой зоне теряет 1 ОС до починки. Яды и огонь броню игнорируют.", "Taking damage: 1) the attacker rolls damage; 2) subtract your armor's SP in that location (body unless the head was aimed at), subtract what's left from HP; 3) if you took any damage, armor in that location loses 1 SP until repaired. Poison and fire bypass armor."),
                    P("ОС от разных слоёв не складывается — считается наибольший источник в зоне; вся броня зоны ломается одновременно. Штраф к РЕФ, ЛВК и СКО берётся один раз — самый тяжёлый из надетых элементов.", "SP from different layers doesn't stack — only the highest source per location counts, and all armor there ablates together. The REF, DEX and MOVE penalty is taken once, from the heaviest piece worn."),
                    T(H("Броня|Armor", "ОС|SP", "Штраф|Penalty", "Цена|Cost"),
                        R("Кожа|Leathers", "4", "—", "{20}"),
                        R("Кевлар®|Kevlar®", "7", "—", "{50}"),
                        R("Лёгкий арморджек|Light Armorjack", "11", "—", "{100}"),
                        R("Костюм Bodyweight|Bodyweight Suit", "11", "—", "{1000}"),
                        R("Средний арморджек|Medium Armorjack", "12", "−2 РЕФ, ЛВК, СКО|−2 REF, DEX, MOVE", "{100}"),
                        R("Тяжёлый арморджек|Heavy Armorjack", "13", "−2 РЕФ, ЛВК, СКО|−2 REF, DEX, MOVE", "{500}"),
                        R("Осколочный бронежилет (Flak)|Flak", "15", "−4 РЕФ, ЛВК, СКО|−4 REF, DEX, MOVE", "{500}"),
                        R("Metalgear®", "18", "−4 РЕФ, ЛВК, СКО|−4 REF, DEX, MOVE", "{5000}")),
                    P("Укрытие: ты в укрытии, только если полностью за предметом, способным остановить пулю, и у врага нет линии видимости. Частичного укрытия нет. Укрытие имеет ПЗ, его можно разрушить атакой по клетке 2 × 2 м; избыток урона пропадает (кроме взрывчатки).", "Cover: you are in cover only fully behind something that stops a bullet and with no line of sight to you. There is no partial cover. Cover has HP and a 2 × 2 m section can be attacked; excess damage is lost (except explosives)."),
                    T(H("Материал|Material", "Толстое, ПЗ|Thick HP", "Тонкое, ПЗ|Thin HP"),
                        R("Сталь|Steel", "50", "25"),
                        R("Камень|Stone", "40", "20"),
                        R("Пуленепробиваемое стекло|Bulletproof Glass", "30", "15"),
                        R("Бетон|Concrete", "25", "10"),
                        R("Дерево|Wood", "20", "5"),
                        R("Штукатурка, пена, пластик|Plaster, Foam, Plastic", "15", "0 (не укрытие|not cover)")),
                    N("Толстое укрытие без ТЕЛО 10+ не сдвинуть. Сталь не берут драка и боевые искусства без киберруки или ТЕЛО 10+.", "Thick cover needs BODY 10+ to move. Steel can't be hurt by Brawling or Martial Arts without a Cyberarm or BODY 10+."),
                    L(new[]
                    {
                        "Щит: надеть и бросить — действие, занимает руку. Подставляя щит под атаку, которую видишь, ты не уклоняешься вовсе; щит принимает весь удар на свои ПЗ. Пуленепробиваемый щит — 10 ПЗ, {100}. Щит из трупа: ПЗ равны ТЕЛО при жизни.",
                        "Живой щит: нужно быть нападающим в захвате и не держать другого щита (действие). Ты в укрытии, но щит не закрывает от рукопашных атак и от выстрелов в голову; он не уклоняется, получает урон как обычно, а умерший становится щитом с ПЗ = ТЕЛО. Отпустить — бесплатно."
                    }, new[]
                    {
                        "Shield: equip and drop take an Action and a hand. Interposing a shield against an attack you can see means you can't dodge at all; the shield takes the whole attack on its HP. Bulletproof Shield: 10 HP, {100}. Corpse shield: HP equal to the BODY it had in life.",
                        "Human Shield: you must be the Grapple attacker and not hold another shield (Action). You are in cover, but it doesn't block Melee Attacks or Aimed head shots; it can't dodge, takes damage normally, and if it dies it becomes a shield with HP = BODY. Letting go is free."
                    })),

                // ------------------------------------------------------------------------------------------------
                B("wounds", "РАНЕНИЯ И СПАСБРОСОК ОТ СМЕРТИ", "WOUNDS AND DEATH SAVES", "186–188",
                    "ранение тяжёлое ранение смертельное спасбросок смерти от смерти штраф спасброска стабилизация wound state death save mortally seriously lightly",
                    T(H("Состояние|Wound State", "Порог|Threshold", "Эффект|Effect", "СЛ стабилизации|Stabilization DV"),
                        R("Лёгкое ранение|Lightly Wounded", "меньше макс. ПЗ|below full HP", "нет|none", "10"),
                        R("Тяжёлое ранение|Seriously Wounded", "меньше ½ ПЗ (вверх)|below ½ HP (round up)", "−2 ко всем действиям|−2 to all Actions", "13"),
                        R("Смертельное ранение|Mortally Wounded", "меньше 1 ПЗ|below 1 HP", "−4 ко всем действиям, −6 СКО (мин. 1), спасбросок от смерти каждый ход|−4 to all Actions, −6 MOVE (min 1), Death Save every Turn", "15"),
                        R("Мёртв|Dead", "провален один спасбросок|one failed Death Save", "—", "—")),
                    P("Каждое новое состояние заменяет эффект предыдущего. В смертельном ранении каждое попадание атакой холодного или дальнобойного оружия даёт критическую травму, а штраф к спасброскам растёт на 1.", "Each new state replaces the effects of the previous one. While Mortally Wounded, every Melee or Ranged hit inflicts a Critical Injury and raises your Death Save Penalty by 1."),
                    F("Спасбросок от смерти: 1d10 меньше ТЕЛО — жив. Выпало 10 — провал. Каждый бросок увеличивает штраф на +1.", "Death Save: roll 1d10 under your BODY to live. A 10 always fails. Each roll raises the Penalty by +1."),
                    P("Бросок делается в начале каждого твоего хода, пока ты смертельно ранен. Провал означает смерть. Штраф копится до стабилизации (возврата к 1 ПЗ), потом сбрасывается до базового, который повышают тяжёлые критические травмы. Падение с небоскрёба без парашюта: спасбросок не нужен, ты мёртв.", "Roll at the start of each of your Turns while Mortally Wounded. A failure is death. The Penalty accumulates until Stabilization (back to 1 HP), then resets to the Base Penalty, which the worst Critical Injuries raise. Falling from a skyscraper with no parachute: no Save, you are dead.")),

                // ------------------------------------------------------------------------------------------------
                B("crits", "КРИТИЧЕСКИЕ ТРАВМЫ", "CRITICAL INJURIES", "187–188, 223",
                    "критическая травма критические травмы оторванная рука нога глаз череп перелом critical injury quick fix",
                    P("Если на двух или более кубах урона атаки холодным или дальнобойным оружием выпало 6, наносится критическая травма: брось 2d6 по таблице тела (по таблице головы — только при прицельной атаке в голову) и перебрасывай, пока не выпадет травма, которой у цели ещё нет. Кроме эффекта, каждая критическая травма наносит 5 бонусного урона напрямую по ПЗ (броню не ломает, зона не влияет) — даже если основной урон не пробил ОС.", "If two or more damage dice from a Melee or Ranged Attack show 6, a Critical Injury is inflicted: roll 2d6 on the Body table (Head table only for an Aimed head shot), re-rolling until the target gets one it doesn't already have. Besides the effect, each Critical Injury deals 5 Bonus Damage straight to HP (no ablation, no location modifier) — even if the attack's damage didn't get through SP."),
                    T(H("2d6", "Травма тела|Body Injury", "Эффект|Effect", "Быстрая помощь|Quick Fix", "Лечение|Treatment"),
                        R("2", "Оторванная рука|Dismembered Arm", "Руки нет, предметы падают. Базовый штраф спасброска +1.|The arm is gone, items drop. Base Death Save Penalty +1.", "—", "Хирургия СЛ17|Surgery DV17"),
                        R("3", "Оторванная кисть|Dismembered Hand", "Кисти нет, предметы падают. Базовый штраф спасброска +1.|The hand is gone, items drop. Base Death Save Penalty +1.", "—", "Хирургия СЛ17|Surgery DV17"),
                        R("4", "Разрыв лёгкого|Collapsed Lung", "−2 СКО (мин. 1). Базовый штраф спасброска +1.|−2 MOVE (min 1). Base Death Save Penalty +1.", "Парамедицина СЛ15|Paramedic DV15", "Хирургия СЛ15|Surgery DV15"),
                        R("5", "Перелом рёбер|Broken Ribs", "В конце хода, если прошёл пешком больше 4 м, снова получаешь бонусный урон напрямую по ПЗ.|At the end of a Turn in which you moved over 4 m on foot, you take the Bonus Damage again.", "Парамедицина СЛ13|Paramedic DV13", "Парамедицина СЛ15 или Хирургия СЛ13|Paramedic DV15 or Surgery DV13"),
                        R("6", "Перелом руки|Broken Arm", "Рукой пользоваться нельзя, предметы падают.|The arm can't be used, items drop.", "Парамедицина СЛ13|Paramedic DV13", "Парамедицина СЛ15 или Хирургия СЛ13|Paramedic DV15 or Surgery DV13"),
                        R("7", "Инородное тело|Foreign Object", "Как у перелома рёбер: бонусный урон при движении больше 4 м.|As Broken Ribs: Bonus Damage when moving over 4 m.", "Первая помощь или Парамедицина СЛ13|First Aid or Paramedic DV13", "Быстрая помощь снимает насовсем|Quick Fix removes it permanently"),
                        R("8", "Перелом ноги|Broken Leg", "−4 СКО (мин. 1).|−4 MOVE (min 1).", "Парамедицина СЛ13|Paramedic DV13", "Парамедицина СЛ15 или Хирургия СЛ13|Paramedic DV15 or Surgery DV13"),
                        R("9", "Разрыв мышц|Torn Muscle", "−2 к атакам ближнего боя.|−2 to Melee Attacks.", "Первая помощь или Парамедицина СЛ13|First Aid or Paramedic DV13", "Быстрая помощь снимает насовсем|Quick Fix removes it permanently"),
                        R("10", "Травма позвоночника|Spinal Injury", "В следующий ход нельзя действовать, только перемещаться. Базовый штраф спасброска +1.|Next Turn you can't take an Action, only Move. Base Death Save Penalty +1.", "Парамедицина СЛ15|Paramedic DV15", "Хирургия СЛ15|Surgery DV15"),
                        R("11", "Раздробленные пальцы|Crushed Fingers", "−4 ко всем действиям этой рукой.|−4 to all Actions with that hand.", "Парамедицина СЛ13|Paramedic DV13", "Хирургия СЛ15|Surgery DV15"),
                        R("12", "Оторванная нога|Dismembered Leg", "Ноги нет, −6 СКО (мин. 1), нельзя уклоняться. Базовый штраф спасброска +1.|The leg is gone, −6 MOVE (min 1), can't dodge. Base Death Save Penalty +1.", "—", "Хирургия СЛ17|Surgery DV17")),
                    T(H("2d6", "Травма головы|Head Injury", "Эффект|Effect", "Быстрая помощь|Quick Fix", "Лечение|Treatment"),
                        R("2", "Потеря глаза|Lost Eye", "−4 к дальним атакам и зрительному восприятию. Базовый штраф спасброска +1.|−4 to Ranged Attacks and sight Perception. Base Death Save Penalty +1.", "—", "Хирургия СЛ17|Surgery DV17"),
                        R("3", "Травма мозга|Brain Injury", "−2 ко всем действиям. Базовый штраф спасброска +1.|−2 to all Actions. Base Death Save Penalty +1.", "—", "Хирургия СЛ17|Surgery DV17"),
                        R("4", "Повреждение глаза|Damaged Eye", "−2 к дальним атакам и зрительному восприятию.|−2 to Ranged Attacks and sight Perception.", "Парамедицина СЛ15|Paramedic DV15", "Хирургия СЛ13|Surgery DV13"),
                        R("5", "Сотрясение|Concussion", "−2 ко всем действиям.|−2 to all Actions.", "Первая помощь или Парамедицина СЛ13|First Aid or Paramedic DV13", "Быстрая помощь снимает насовсем|Quick Fix removes it permanently"),
                        R("6", "Перелом челюсти|Broken Jaw", "−4 ко всем действиям, связанным с речью.|−4 to all Actions involving speech.", "Парамедицина СЛ13|Paramedic DV13", "Парамедицина или Хирургия СЛ13|Paramedic or Surgery DV13"),
                        R("7", "Инородное тело|Foreign Object", "Бонусный урон при движении больше 4 м.|Bonus Damage when moving over 4 m.", "Первая помощь или Парамедицина СЛ13|First Aid or Paramedic DV13", "Быстрая помощь снимает насовсем|Quick Fix removes it permanently"),
                        R("8", "Хлыстовая травма шеи|Whiplash", "Базовый штраф спасброска +1.|Base Death Save Penalty +1.", "Парамедицина СЛ13|Paramedic DV13", "Парамедицина или Хирургия СЛ13|Paramedic or Surgery DV13"),
                        R("9", "Трещина черепа|Cracked Skull", "Прицельные выстрелы в голову множат урон × 3 вместо × 2. Базовый штраф спасброска +1.|Aimed head shots multiply damage × 3 instead of × 2. Base Death Save Penalty +1.", "Парамедицина СЛ15|Paramedic DV15", "Парамедицина или Хирургия СЛ15|Paramedic or Surgery DV15"),
                        R("10", "Повреждение уха|Damaged Ear", "Если за ход прошёл пешком больше 4 м, в следующий ход нельзя перемещаться; −2 к слуховому восприятию.|If you moved over 4 m on foot, you can't Move next Turn; −2 to hearing Perception.", "Парамедицина СЛ13|Paramedic DV13", "Хирургия СЛ13|Surgery DV13"),
                        R("11", "Раздавленная трахея|Crushed Windpipe", "Говорить нельзя. Базовый штраф спасброска +1.|You can't speak. Base Death Save Penalty +1.", "—", "Хирургия СЛ15|Surgery DV15"),
                        R("12", "Потеря уха|Lost Ear", "Как повреждение уха, но −4 к слуху. Базовый штраф спасброска +1.|As Damaged Ear but −4 to hearing. Base Death Save Penalty +1.", "—", "Хирургия СЛ17|Surgery DV17")),
                    N("Быстрая помощь снимает эффект до конца дня (1 минута на попытку, можно на себе). Лечение снимает эффект навсегда (4 часа на попытку, на себе нельзя). Для травм киберчастей вместо этих навыков годится кибертехника.", "A Quick Fix removes the effect for the rest of the day (1 minute per attempt, can be done on yourself). A Treatment removes it permanently (4 hours per attempt, not on yourself). For cyberware parts, Cybertech can replace those Skills.")),

                // ------------------------------------------------------------------------------------------------
                B("hazards", "ДРУГИЕ ИСТОЧНИКИ УРОНА", "OTHER WAYS TO GET HURT", "180–181",
                    "горение огонь утопление удушье электричество падение яд наркотик радиация воздействие среды атлетика сопротивление пыткам fire drowning electrocution falling poison radiation exposure",
                    T(H("Источник|Source", "Эффект|Effect"),
                        R("Горение: слабое (деревянный пожар)|On Fire: Mild (wood fire)", "2 урона напрямую по ПЗ в конце хода, броня не ломается; тушение — действие.|2 damage direct to HP at the end of your Turn, no ablation; putting out costs an Action."),
                        R("Горение: сильное (бензин)|On Fire: Strong (gasoline)", "4 урона напрямую по ПЗ.|4 damage direct to HP."),
                        R("Горение: смертельное (термит)|On Fire: Deadly (thermite)", "6 урона напрямую по ПЗ.|6 damage direct to HP."),
                        R("Утопление|Drowning", "Дыхание задерживается на ТЕЛО минут; потом в начале хода урон = ТЕЛО напрямую по ПЗ. Удушье в вакууме: ещё 1d6 урона по ИНТ, РЕФ и ЛВК, при ИНТ 0 — смерть.|You hold your breath for BODY minutes; then at the start of each Turn take BODY direct to HP. Suffocating in vacuum: also 1d6 to INT, REF and DEX; INT 0 means death."),
                        R("Электричество|Electrocution", "6d6 сразу (броня учитывается) и снова в конце каждого хода, пока не отойдёшь от источника.|6d6 at once (armor applies) and again at the end of each Turn until you leave the source."),
                        R("Воздействие среды|Exposure", "1d6 напрямую по ПЗ за каждый день, естественное лечение невозможно. Подходящее снаряжение отменяет риск.|1d6 direct to HP per day, no natural healing. Proper gear removes the risk."),
                        R("Падение|Falling", "Падают на 40 м за ход. Спасти от падения — Атлетика СЛ15 (у края). Удар: за каждые 10 м (от 10 м) 2d6 через броню и перелом ноги, если не пройдена Атлетика СЛ15. Две киберноги спасают при падении до 30 м.|You fall 40 m per Turn. Saving yourself at a ledge: Athletics DV15. Impact: for every 10 m (from 10 m) 2d6 soaked by armor and Broken Leg unless Athletics DV15 is made. Two Cyberlegs save you from falls up to 30 m."),
                        R("Радиация|Radiation", "В горячей зоне каждый ход как «слабое горение» (в очень горячей — «смертельное»), потушить нельзя.|In a hot zone every Turn counts as Mild On Fire (Deadly in an extremely hot zone), and you can't put it out.")),
                    T(H("Яд|Poison", "Сопротивление пыткам/наркотикам|Resist Torture/Drugs", "Эффект|Effect"),
                        R("Слабый (белладонна)|Mild (belladonna)", "СЛ 11|DV11", "1d6 напрямую по ПЗ|1d6 direct to HP"),
                        R("Сильный (мышьяк)|Strong (arsenic)", "СЛ 13|DV13", "2d6 напрямую по ПЗ|2d6 direct to HP"),
                        R("Смертельный (биотоксин)|Deadly (biotoxin)", "СЛ 15|DV15", "3d6 напрямую по ПЗ|3d6 direct to HP")),
                    N("Огонь, радиация, яды и наркотики критических травм не вызывают, если описание прямо не говорит обратного.", "Fire, radiation, poisons and drugs can't cause Critical Injuries unless their description says so.")),

                // ------------------------------------------------------------------------------------------------
                B("vehicles", "БОЙ НА ТРАНСПОРТЕ", "VEHICLE COMBAT", "189–192",
                    "транспорт вождение манёвр таран потеря контроля псп слабое место vehicle driving maneuver ramming sdp",
                    P("Транспорт имеет ПСП (его ПЗ). С 1 ПСП и выше он ездит; при 0 — уничтожен, перестаёт быть укрытием. Транспорт не уклоняется, но человек внутри уклоняется от направленных в него атак как обычно. Стекло не даёт укрытия. Слабое место (шина, двигатель, бак): прицельная атака −8, прошедший урон × 2; по неподвижному транспорту рукопашная атака попадает сама, по едущему нужна СЛ 13.", "A vehicle has SDP (its HP). With 1+ SDP it runs; at 0 it is Destroyed and no longer cover. Vehicles can't dodge, but a person inside can dodge attacks aimed at them. Glass gives no cover. Weak points (tires, engine, gas cap): Aimed Shot −8, damage through SP × 2; Melee hits a stationary vehicle automatically, a moving one needs DV13."),
                    L(new[]
                    {
                        "Сесть и завести — действия. Водитель сразу встаёт в начало очереди, СКО транспорта становится его СКО (штрафы водителя не влияют), «бег» недоступен. Без интерфейсных разъёмов одна рука занята рулём: убрал её — на следующем ходу теряешь контроль.",
                        "Обычное вождение не требует проверки, если РЕФ + навык управления больше 9. Иначе каждый ход тратишь действие на проверку СЛ 10.",
                        "Манёвр занимает действие и перемещение; провал РЕФ + управление + 1d10 — потеря контроля (рефери решает движение хода).",
                        "Таран: транспорт и цель получают по 6d6, все участники аварии получают хлыстовую травму шеи. Если ПЗ цели упало до 0, движение продолжается. Пешком увернуться от тарана — ЛВК + уклонение + 1d10 против СЛ 13 (можно оказаться на крыше)."
                    }, new[]
                    {
                        "Getting in and starting are Actions. The driver jumps to the top of the Initiative Queue, the vehicle's MOVE becomes theirs (their penalties don't apply), and Run is unavailable. Without Interface Plugs one hand is on the wheel: take it off and next Turn you lose control.",
                        "Basic driving needs no Check if REF + Control Skill is above 9. Otherwise you spend your Action each Turn on a DV10 Check.",
                        "A Maneuver takes your Action and Move Action; failing REF + Control Skill + 1d10 means Losing Control (the GM decides your movement).",
                        "Ramming: the vehicle and the target both take 6d6 and everyone in the crash suffers Whiplash. If the target's HP hit 0, movement continues. On foot, dodging a ram is DEX + Evasion + 1d10 vs DV13 (you may land on the roof)."
                    }),
                    T(H("Манёвр|Maneuver", "СЛ|DV"),
                        R("Резкий манёвр|Swerve", "13"),
                        R("Крутой поворот|Sharp Turn", "13"),
                        R("Экстренная остановка|Emergency Stop", "13"),
                        R("Дрифт|Bootleg Turn", "17"),
                        R("Прыжок|Do a Jump", "17"),
                        R("Посадка (воздух)|Landing (air)", "13"),
                        R("Фигурный манёвр (воздух)|Aerobatic Maneuver (air)", "17"))),

                // ------------------------------------------------------------------------------------------------
                B("rep", "РЕПУТАЦИЯ И РАЗБОРКИ", "REPUTATION AND FACEDOWNS", "193–194",
                    "репутация разборка facedown reputation",
                    P("Репутацию присваивает рефери за поступок; новая заменяет старую, только если уровень выше. Большинство героев стартует с 0. При встрече с новыми людьми брось 1d10: если выпало меньше уровня репутации того, кого встречаешь, о нём слышали. Позорный поступок тоже даёт уровень и может заменить славу.", "The GM awards Reputation for a deed; a new one replaces the old only if the Level is higher. Most characters start at 0. When meeting new people roll 1d10: under the other person's Reputation Level means they've heard of them. A shameful deed also gets a Level and can replace fame."),
                    T(H("Уровень|Level", "Кто о тебе знает|Who knows about you"),
                        R("1", "Все, кто был при этом.|Anyone who was there."),
                        R("2", "Ближайшие друзья.|Immediate friends."),
                        R("3", "Коллеги и знакомые.|Co-workers and acquaintances."),
                        R("4", "Весь район.|The local area."),
                        R("5", "Знают имя за пределами района.|Name known beyond the local area."),
                        R("6", "Узнают в лицо за пределами района.|Recognized on sight beyond the local area."),
                        R("7", "Пара новостных заметок.|A news story or two."),
                        R("8", "Регулярно в заголовках.|Regularly in headlines."),
                        R("9", "Всегда на ТВ и в скримлистах.|Always on TV and screamsheets."),
                        R("10", "Известен во всём мире.|Known worldwide.")),
                    F("Разборка: КРУТ + Репутация + 1d10 против КРУТ + Репутация + 1d10", "Facedown: COOL + Reputation + 1d10 vs COOL + Reputation + 1d10"),
                    P("Ничья — ничего не происходит. Проигравший отступает или получает −2 ко всем будущим действиям против этого противника, пока не победит его хотя бы раз. Если репутация — за трусость, уровень считается отрицательным.", "A tie changes nothing. The loser backs down or takes −2 to all future Actions against that opponent until beating them once. A Reputation earned for cowardice counts as negative.")),

                // ------------------------------------------------------------------------------------------------
                B("healing", "ЛЕЧЕНИЕ И БОЛЬНИЦА", "HEALING AND HOSPITALS", "222–228",
                    "лечение регенерация стабилизация первая помощь парамедицина хирургия кибертехника медицинские технологии больница trauma team бодибанк наркотики healing stabilize hospital drugs",
                    F("Стабилизация (действие): ТЕХ + Первая помощь или Парамедицина + 1d10 против СЛ состояния (10 / 13 / 15)", "Stabilization (Action): TECH + First Aid or Paramedic + 1d10 vs the state's DV (10 / 13 / 15)"),
                    L(new[]
                    {
                        "После стабилизации цель лечит ПЗ = ТЕЛО за каждые полные сутки спокойного отдыха до полных ПЗ. Если пациент напрягается — в этот день ПЗ нет, раны раскрываются, нужна новая стабилизация.",
                        "Смертельно раненый после стабилизации сразу поднимается до 1 ПЗ и теряет сознание ровно на 1 минуту; спасброски от смерти при новых ранениях он всё равно делает.",
                        "Навыки лечения: кибертехника (только киберчасти, не стабилизирует), первая помощь (стабилизация и самые простые травмы, у всех минимум +2), парамедицина (x2, почти всё), хирургия (самое тяжёлое, только у медтехов).",
                        "Больница берёт плату только за самую высокую СЛ стабилизации или лечения за визит. Койка — 100eb за ночь.",
                        "Trauma Team: Silver 500eb в месяц, Executive 1 000eb (хирургия за их счёт). Вызов — действие; бросай 1d6 — столько раундов до прибытия."
                    }, new[]
                    {
                        "Once stabilized, the target heals BODY HP per full day of quiet rest until at full HP. If the patient pushes it, no HP that day, the wounds reopen and they need to be stabilized again.",
                        "A Mortally Wounded target who is stabilized is immediately raised to 1 HP and Unconscious for exactly 1 minute; they still roll Death Saves if hurt again.",
                        "Healing Skills: Cybertech (cyberware only, can't stabilize), First Aid (stabilize and simple injuries, everyone has at least +2), Paramedic (x2, nearly everything), Surgery (the worst, Medtechs only).",
                        "A hospital charges only for the highest-DV Stabilization or Treatment of the visit. A bed costs 100eb a night.",
                        "Trauma Team: Silver 500eb a month, Executive 1,000eb (surgery covered). Calling is an Action; roll 1d6 for the Rounds until they arrive."
                    }),
                    T(H("Самая высокая СЛ за визит|Highest DV of the visit", "Цена|Cost"),
                        R("СЛ 10|DV10", "{50}"),
                        R("СЛ 13|DV13", "{100}"),
                        R("СЛ 15|DV15", "{500}"),
                        R("СЛ 17+|DV17+", "{1000}")),
                    T(H("Место установки импланта|Typical installation", "СЛ операции (медтех)|Surgery DV (Medtech)", "В больнице|At a hospital"),
                        R("Торговый центр|Mall", "13", "{100}"),
                        R("Клиника|Clinic", "15", "{500}"),
                        R("Больница|Hospital", "17", "{1000}")),
                    T(H("Наркотик|Drug", "Основной эффект|Primary Effect", "СЛ вторичного|Secondary DV", "Доза|Dose"),
                        R("Чёрное кружево|Black Lace", "24 часа|24 hours", "17", "{50}"),
                        R("Голубое стекло|Blue Glass", "4 часа|4 hours", "15", "{20}"),
                        R("Буст|Boost", "24 часа|24 hours", "17", "{50}"),
                        R("Смэш|Smash", "4 часа|4 hours", "15", "{10}"),
                        R("Синткок|Synthcoke", "4 часа|4 hours", "15", "{20}")),
                    N("После основного эффекта бросок ВОЛЯ + сопротивление пыткам/наркотикам + 1d10 против СЛ вторичного эффекта. Новая доза продлевает основной эффект на полный срок.", "When the Primary Effect ends roll WILL + Resist Torture/Drugs + 1d10 against the Secondary Effect DV. Another dose extends the Primary Effect by its full duration.")),

                // ------------------------------------------------------------------------------------------------
                B("humanity", "ЧЕЛОВЕЧНОСТЬ, ТЕРАПИЯ, КИБЕРПСИХОЗ", "HUMANITY, THERAPY, CYBERPSYCHOSIS", "79–80, 229–232",
                    "человечность потеря терапия киберпсихоз эмпатия имплант humanity loss therapy cyberpsychosis empathy",
                    P("Потеря человечности: установка киберимплантов (по таблице импланта) и тяжёлые события. Медицинский киберимплант взамен утраченной части потери не даёт. Киберимплант с 0 потерь максимум не снижает.", "Humanity Loss comes from installing cyberware (per the item's HL) and traumatic events. Medical-grade cyberware replacing a lost part causes none. Cyberware with 0 HL doesn't lower maximum Humanity."),
                    T(H("Травма|Trauma", "Потеря|Loss"),
                        R("Телесная травма (пытки)|Traumatic Physical Incident (torture)", "1d6"),
                        R("Психическая травма (жуткая смерть, увечье)|Traumatic Mental Incident (horrific death, mutilation)", "1d6"),
                        R("Долгий стресс (плен, длительное насилие)|Long-term Mental Stress (kidnapping, abuse)", "2d6"),
                        R("Долгий стресс среды (голод, зона боевых действий, месяц в опасности)|Long-term Environmental Stress (starvation, war zone, a month of danger)", "2d6")),
                    T(H("Терапия (1 неделя, делает медтех, не на себе)|Therapy (1 week, Medtech, not on self)", "Цена|Cost", "СЛ медтеха|Medtech DV", "Материалы|Materials", "Эффект|Effect"),
                        R("Зависимость|Addiction", "{1000}", "15", "{500}", "одна зависимость снята; год после этого проваливаются броски вторичного эффекта|one addiction cured; for a year, Secondary Effect rolls automatically fail"),
                        R("Обычная потеря человечности|Standard Humanity Loss", "{500}", "15", "{100}", "+2d6 человечности|regain 2d6 Humanity"),
                        R("Крайняя потеря человечности|Extreme Humanity Loss", "{1000}", "17", "{500}", "+4d6 человечности|regain 4d6 Humanity")),
                    N("Полностью восстановить человечность без удаления имплантов нельзя: каждый имплант снижает максимум на 2, борг-имплант на 4. Провал — неделя и материалы потеряны.", "Humanity can't be fully regained without removing cyberware: each piece lowers the maximum by 2, borgware by 4. On a failure the week and the materials are lost."),
                    T(H("Если ЭМП…|If EMP is…", "Состояние|State"),
                        R("3 и больше|3 or more", "Киберпсихоза и диссоциации нет.|No cyberpsychosis or dissociative disorder."),
                        R("2", "Пограничное диссоциативное расстройство, отыгрывай.|Borderline dissociative disorder, roleplay it."),
                        R("1", "Диссоциативное расстройство, ярко выражены не меньше 3 признаков психопатии по Харе.|Dissociative disorder, at least 3 Hare Psychopathy traits strongly shown."),
                        R("0", "Киберпсихоз, не меньше 5 признаков.|Cyberpsychosis, at least 5 traits."),
                        R("0 и человечность отрицательна|0 and negative Humanity", "Крайний киберпсихоз: персонаж у рефери, возвращается игроку, когда человечность снова положительна.|Extreme cyberpsychosis: the GM plays the character until Humanity is positive again."))),

                // ------------------------------------------------------------------------------------------------
                B("net", "НЕТРАННИНГ", "NETRUNNING", "196–202",
                    "нетраннинг сеть интерфейс сетевое действие кибердека программа скан бэкдор плащ контроль вирус разряд следопыт подкат netrun interface net action cyberdeck",
                    P("Нужны нейросвязь, интерфейсные разъёмы, кибердека и очки виртуальности (без них нетраннер фактически без сознания). Один нетраннер подключён только к одной деке; смена деки — мясное действие.", "You need a Neural Link, Interface Plugs, a Cyberdeck and Virtuality Goggles (without them you are effectively Unconscious). A netrunner connects to one Cyberdeck at a time; switching decks is a Meat Action."),
                    T(H("Кибердека|Cyberdeck", "Цена|Cost", "Слоты|Slots"),
                        R("Низкого качества|Poor Quality", "{100}", "5"),
                        R("Обычная|Standard", "{500}", "7"),
                        R("Отличного качества|Excellent Quality", "{1000}", "9")),
                    P("В свой ход нетраннер выполняет либо одно мясное действие, либо столько сетевых действий, сколько даёт его Интерфейс; действие перемещения остаётся.", "On your Turn you take either one Meat Action or as many NET Actions as your Interface allows; you keep your Move Action."),
                    T(H("Ранг Интерфейса|Interface Rank", "1–3", "4–6", "7–9", "10"),
                        R("Сетевых действий|NET Actions", "2", "3", "4", "5")),
                    L(new[]
                    {
                        "Сетевые действия: вход и выход (в пределах 6 м от точки доступа; стены блокируют), способность интерфейса, активация или деактивация программы, прочее. Движение по архитектуре и копирование файла действия не требуют.",
                        "Выход без «выхода» оставляет тебя под эффектом всего нерассеянного чёрного льда. Безопасный выход сбрасывает защиту архитектуры, все программы уходят с тобой.",
                        "Программа: каждую активируют раз за мясной раунд, атакующие деактивируются сами. При 0 ЦЕЛ программа нецелостна: деактивировать и активировать снова — 2 сетевых действия. Установка или удаление программы — 1 час."
                    }, new[]
                    {
                        "NET Actions: Jack In/Out (within 6 m/yds of an access point; walls block), an Interface Ability, Activate/Deactivate a Program, or miscellaneous. Moving in the Architecture and copying a File cost no NET Action.",
                        "Leaving without Jacking Out leaves you suffering all remaining Black ICE you met. A proper Jack Out resets the Architecture's defenses and your Programs leave with you.",
                        "Programs: each can be activated once per Meatspace Round, Attackers deactivate themselves. At 0 REZ a Program is Derezzed: deactivating and re-activating it costs 2 NET Actions. Installing or uninstalling takes 1 hour."
                    }),
                    F("Способность интерфейса: Интерфейс + 1d10 против СЛ (разряд — против защиты)", "Interface Ability: Interface + 1d10 vs DV (Zap vs defense)"),
                    T(H("Способность|Ability", "Что делает|What it does"),
                        R("Сканер|Scanner", "Мясное действие: найти физические точки доступа поблизости.|Meat Action: find nearby access points."),
                        R("Бэкдор|Backdoor", "Взломать пароль (если пароль известен, проходишь сам).|Break a Password (known passwords pass automatically)."),
                        R("Плащ|Cloak", "Скрыть следы и вирус; СЛ следопыта для других = твой результат плаща.|Hide your traces and virus; other netrunners' Pathfinder DV = your Cloak result."),
                        R("Контроль|Control", "Захватить узел управления (камеры, турели, лифты…); каждый узел раз за ход, отдельные действия — отдельные сетевые действия.|Seize a Control Node (cameras, turrets, elevators…); once per Turn each, operating things costs separate NET Actions."),
                        R("Идентификация|Eye-Dee", "Узнать, что за данные и чего они стоят.|Learn what data is and its worth."),
                        R("Следопыт|Pathfinder", "Показывает карту: этажей столько, каков результат, до первой преграды выше результата.|Reveals the map: as many floors as your Check, up to the first obstruction with a higher DV."),
                        R("Подкат|Slide", "Уйти от одного не-демонского чёрного льда: проверка против его Восприятия + 1d10, раз за ход, превентивно нельзя.|Flee one non-Demon Black ICE: check against its Perception + 1d10, once per Turn, not preemptively."),
                        R("Вирус|Virus", "На нижнем уровне оставить вирус (до 2 изменений); СЛ назначает рефери, уничтожить — СЛ = результат создания.|At the lowest floor leave a Virus (up to 2 changes); GM sets the DV, destroying it takes your creation result."),
                        R("Разряд|Zap", "Атака сетевым действием: против защиты программы + 1d10 или Интерфейса нетраннера + 1d10, 1d6 по ЦЕЛ или напрямую по мозгу.|Attack as a NET Action: vs a Program's Defense + 1d10 or a netrunner's Interface + 1d10, 1d6 to REZ or straight to the brain.")),
                    F("Сетевой бой: Интерфейс + АТК программы + 1d10 (или АТК чёрного льда + 1d10) против Интерфейс + 1d10 (или ЗАЩ программы + 1d10)", "NET combat: Interface + Program ATK + 1d10 (or Black ICE ATK + 1d10) vs Interface + 1d10 (or Program DEF + 1d10)"),
                    N("Программы делятся на усиливающие, защитные и атакующие. Программа против типа цели (анти-пользователь, анти-программа) действует только на свою цель.", "Programs are Boosters, Defenders and Attackers. A Program against a target type (Anti-Personnel, Anti-Program) only affects that target.")),

                // ------------------------------------------------------------------------------------------------
                B("improve", "ПРОКАЧКА: ОЧКИ УЛУЧШЕНИЯ", "IMPROVEMENT POINTS", "408–413|408–411",
                    "прокачка очки улучшения улучшение навык ранг ролевой навык мультикласс improvement points ip cost skill rank multiclass",
                    P("После сессии рефери выдаёт очки по успеху группы и стилю игрока: 10 — старались, но не вышло; 30 — слаженная работа; 50 — очень умно; 80 — легендарно. Навык или ранг повышают по одному уровню за раз, перескакивать нельзя.", "After each session the GM awards points by group success and playstyle: 10 for a valiant try, 30 for good teamwork, 50 for very clever play, 80 for legendary. Skills and Ranks go up one Level at a time, no skipping."),
                    T(H("Уровень / ранг (следующий)|Next Level / Rank", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"),
                        R("Навык|Skill", "20", "40", "60", "80", "100", "120", "140", "160", "180", "200"),
                        R("Навык (x2)|Skill (x2)", "40", "80", "120", "160", "200", "240", "280", "320", "360", "400"),
                        R("Ролевой навык|Role Ability", "60", "120", "180", "240", "300", "360", "420", "480", "540", "600")),
                    N("Сменить роль можно, когда прошлая роль на ранге 4+: купи ранг 1 новой за очки. Новая роль начинается с ранга 1, и мультикласс заблокирован, пока она не дойдёт до ранга 4.", "You may switch Roles once the old one is at Rank 4+: buy Rank 1 of the new one with Improvement Points. It starts at Rank 1 and multiclassing is locked until it reaches Rank 4.")),

                // ------------------------------------------------------------------------------------------------
                B("money", "ДЕНЬГИ, ОБРАЗ ЖИЗНИ И ЖИЛЬЁ", "MONEY, LIFESTYLE AND HOUSING", "380–381, 388|377–379, 385",
                    "деньги цена категория образ жизни жильё аренда сон выносливость евробаксы eb price category lifestyle housing rent sleep fatigue",
                    P("Всё покупается по одной из восьми ценовых категорий. Без фиксера или ночного рынка доступно только до Премиума включительно; без фиксера скидок нет.", "Everything falls into one of eight Price Categories. Without a Fixer or Night Market only items up to Premium can be bought, and there are no discounts without a Fixer."),
                    T(H("Категория|Category", "Цена|Price"),
                        R("Дёшево|Cheap", "10eb"),
                        R("Повседневно|Everyday", "20eb"),
                        R("Недёшево|Costly", "50eb"),
                        R("Премиум|Premium", "100eb"),
                        R("Дорого|Expensive", "500eb"),
                        R("Очень дорого|Very Expensive", "1 000eb"),
                        R("Роскошь|Luxury", "5 000eb"),
                        R("Супер роскошь|Super Luxury", "10 000eb+")),
                    P("В начале каждого месяца платят за жильё и образ жизни. Не оплатил образ жизни — неделя на долг, потом каждый день спасбросок от смерти. Если выбирать, лучше выселение, чем голод.", "At the start of each month you pay for Housing and Lifestyle. Skip your Lifestyle and you have a week, then a Death Save every day. If you must choose, better evicted than starved."),
                    T(H("Образ жизни|Lifestyle", "В месяц|Per month"),
                        R("Киббл|Kibble", "100eb"),
                        R("Обычные полуфабрикаты|Generic Prepak", "300eb"),
                        R("Хорошие полуфабрикаты|Good Prepak", "600eb"),
                        R("Свежая еда|Fresh Food", "1 500eb")),
                    T(H("Жильё|Housing", "Аренда в месяц|Rent per month", "Покупка|Purchase"),
                        R("Куб-отель|Cube Hotel", "500eb", "—"),
                        R("Грузовой контейнер|Cargo Container", "1 000eb", "15 000eb"),
                        R("Студия|Studio Apartment", "1 500eb", "25 000eb"),
                        R("Квартира с двумя спальнями|Two-Bedroom Apartment", "2 500eb", "35 000eb"),
                        R("Корпоративный конапт|Corporate Conapt", "даёт корпорация|given by a Corp", "—"),
                        R("Улучшенный конапт|Upscale Conapt", "7 500eb", "85 000eb"),
                        R("Роскошный пентхаус|Luxury Penthouse", "15 000eb", "150 000eb"),
                        R("Корпоративный дом в Бивервилле|Corporate Beaverville House", "даёт корпорация|given by a Corp", "200 000eb"),
                        R("Корпоративный особняк в Бивервилле|Corporate Beaverville McMansion", "даёт корпорация|given by a Corp", "500 000eb")),
                    L(new[]
                    {
                        "Меньше 6 часов сна в сутки: −2 ко всем действиям за каждый такой день, пока не выспишься.",
                        "Неудобный сон (теснота: людей больше, чем 1 + число спален; сон на улице без Выносливости СЛ 15; в дикой местности без Выживания СЛ 15) оставляет измотанным: −2 ко всем действиям на следующий день. В кубе сон вдвоём всегда неудобен."
                    }, new[]
                    {
                        "Under 6 hours of sleep a day: −2 to everything for each day since you last slept a full 6 hours.",
                        "Sleeping uncomfortably (crammed: more people than 1 + bedrooms; on the street without Endurance DV15; in the wilderness without Wilderness Survival DV15) leaves you fatigued: −2 to everything the next day. Two people in a cube is always uncomfortable."
                    }))
            };
        }

        // Расхождения русского и английского изданий (в тексте выше взято значение оригинала):
        //  • Критические травмы тела: «Перелом рёбер», «Перелом руки», «Перелом ноги» — лечение в РУ «Хирургия СЛ17», в EN «Surgery DV13».
        //  • «Оторванная нога»: лечение в РУ «Хирургия СЛ15», в EN «Surgery DV17».
        //  • Критические травмы головы: «Повреждение уха» в РУ — бонусный урон при движении, в EN — запрет перемещения в следующий ход и −2 к слуху;
        //    «Потеря уха» в РУ — бонусный урон при движении, в EN — запрет перемещения и −4 к слуху.
        //  • Таран (стр. 192): в РУ «травма трахеи» (раздавленная трахея), в EN «Whiplash» (хлыстовая травма шеи).
        //  • Trauma Team (стр. 224): у медицинского ассистента и пилота СКО и ТЕЛО в РУ 4, в EN 6.
        //  • Стр. 126–127: инициатива в РУ один раз записана «РЕА + 1d10» (в остальных местах «РЕФ»).
        //  • Ссылки на «Покупку и продажу»: РУ издание — стр. 388, EN — стр. 385 (другая пагинация).
        //  • Названия ценовых категорий в РУ различаются между главами (в «Перестрелке…» «Очень дорого» на 500eb, в «Новой экономике» «Дорого»).
    }
}
