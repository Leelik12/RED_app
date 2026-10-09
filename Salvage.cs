using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    // сбор обломков (DLC «Сбор обломков Найт-Сити»): неделя поисков, проверка технического навыка, стоимость хлама
    public static class Salvage
    {
        public static readonly string[] Categories =
        {
            "Киберснаряжение", "Оружие", "Стандартное снаряжение", "Стандартное снаряжение", "Броня", "Кибердеки и программы"
        };

        // цена хлама → СЛ (любая категория), СЛ (конкретная категория); 0 — недоступно
        public static readonly int[] Prices = { 10, 20, 50, 100, 500, 1000 };
        public static readonly int[] DcAny = { 9, 13, 15, 17, 24, 29 };
        public static readonly int[] DcSpecific = { 13, 15, 17, 21, 29, 0 };

        public static readonly string[] Hazards =
        {
            "Над вами рухнула конструкция: урон 6d6 (− ОС брони) и критическая травма (таблица критических повреждений тела).",
            "Вы попадаете в перестрелку: урон 6d6 (− ОС брони).",
            "Высокий уровень радиации в течение 1d10 раундов. Радиационный костюм блокирует эффект.",
            "Под вами обрушился пол, падение на 30 м: урон 6d6 (− ОС брони). Рука-крюк или пистолет-крюк позволяют ухватиться за край.",
            "Ядовитый газ (биотоксин, 1 раунд). Противогаз или подача кислорода блокируют эффект.",
            "Вы касаетесь провода под напряжением и получаете удар током на 1 раунд."
        };

        public const string Rules =
            "Попытка сбора занимает неделю и несовместима с другой деятельностью (лечение, халтурки).\n" +
            "1. Категория: конкретная — сразу шаг 2; иначе бросок 1d6 по таблице категорий.\n" +
            "2. Проверка базового технического навыка: чем больше итог, тем дороже улов (СЛ зависит от того, ищете ли вы конкретную категорию). " +
            "Критический провал (1 на 1d10) — не просто пусто, а опасность (1d6).\n" +
            "3. Выберите в категории предметы на сумму не больше стоимости улова; лишняя стоимость теряется. " +
            "Находки уничтожены (не работают), но чинятся подходящим ТЕХ-навыком; без возможности ремонта — не чинятся вовсе.";

        public static string Category(int d6) => Categories[Math.Max(1, Math.Min(6, d6)) - 1];

        public static string Hazard(int d6) => Hazards[Math.Max(1, Math.Min(6, d6)) - 1];

        // самая большая стоимость хлама, которую даёт итог проверки; 0 — ничего не найдено
        public static int Value(int total, bool specific)
        {
            int best = 0;
            for (int i = 0; i < Prices.Length; i++)
            {
                int dc = specific ? DcSpecific[i] : DcAny[i];
                if (dc > 0 && total >= dc) best = Prices[i];
            }
            return best;
        }

        // что можно выбрать в категории (1..6 как в таблице): (название, цена) с ценой не выше стоимости улова
        public static List<KeyValuePair<string, int>> Eligible(int category, int value)
        {
            var res = new List<KeyValuePair<string, int>>();
            switch (category)
            {
                case 1:
                    res.AddRange(Packs.Enabled("cyber").Where(i => i.Cost > 0 && i.Cost <= value).Select(i => new KeyValuePair<string, int>(i.Name, i.Cost)));
                    break;
                case 2:
                    res.AddRange(GearCatalog.Weapons.Where(w => w.Cost > 0 && w.Cost <= value).Select(w => new KeyValuePair<string, int>(w.Name, w.Cost)));
                    break;
                case 3:
                case 4:
                    res.AddRange(GearCatalog.Gear.Where(g => g.Cost > 0 && g.Cost <= value && g.Group != "Боеприпасы" && !IsDeck(g.Name)).Select(g => new KeyValuePair<string, int>(g.Name, g.Cost)));
                    break;
                case 5:
                    res.AddRange(GearCatalog.Armor.Where(a => a.Cost > 0 && a.Cost <= value).Select(a => new KeyValuePair<string, int>(a.Name, a.Cost)));
                    break;
                case 6:
                    res.AddRange(GearCatalog.Gear.Where(g => g.Cost > 0 && g.Cost <= value && IsDeck(g.Name)).Select(g => new KeyValuePair<string, int>(g.Name, g.Cost)));
                    break;
            }
            return res.OrderByDescending(p => p.Value).ThenBy(p => p.Key).ToList();
        }

        private static bool IsDeck(string name) => name.IndexOf("кибердек", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("программ", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
