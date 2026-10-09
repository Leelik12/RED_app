using System;
using System.Collections.Generic;

namespace CyberpunkRED_Generator
{
    // погода Найт-Сити (DLC «Погода в Найт-Сити»): два броска 1d6 по таблице месяца (температура и условия),
    // при 6 на условиях — «странная погода» (1d10) и её длительность
    public class WeatherResult
    {
        public int Month;
        public int TempRoll;
        public int CondRoll;
        public string Temperature;      // «Холодно (около 4°C)»
        public string TempClass;        // Холодно / Прохладно / Тепло / Жарко / Экстремальная жара / Внезапное похолодание
        public string Condition;        // «Ясно», «Пасмурно», …
        public int StrangeRoll;         // 0, если нет
        public string Strange;          // «Кислотный дождь» или null
        public string Duration;         // «3 ч» (ровно как выпало)
        public string Rules;            // механика для текущей погоды
    }

    public static class Weather
    {
        public static readonly string[] MonthNames =
        {
            "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
            "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
        };

        public const string StrangeMark = "Странная погода";

        private class Season
        {
            public string[] Temps;   // индексы 0..5 = результат 1d6 − 1
            public string[] Conds;
        }

        private static readonly Season Winter = new Season
        {
            Temps = new[] { "Холодно (около 2°C)", "Холодно (около 4°C)", "Холодно (около 4°C)", "Прохладно (около 10°C)", "Прохладно (около 10°C)", "Прохладно (около 15°C)" },
            Conds = new[] { "Ясно", "Небольшой дождь/Слякоть", "Пасмурно", "Пасмурно", "Сильный дождь/Мокрый снег", StrangeMark }
        };

        private static readonly Season Spring = new Season
        {
            Temps = new[] { "Холодно (около 4°C)", "Прохладно (около 10°C)", "Прохладно (около 10°C)", "Прохладно (около 10°C)", "Тепло (около 15°C)", "Тепло (около 21°C)" },
            Conds = new[] { "Ясно", "Небольшой дождь/Слякоть", "Пасмурно", "Небольшой дождь/Слякоть", "Ливень", StrangeMark }
        };

        private static readonly Season Summer = new Season
        {
            Temps = new[] { "Тепло (около 15°C)", "Тепло (около 21°C)", "Тепло (около 21°C)", "Жарко (около 27°C)", "Жарко (около 27°C)", "Жарко (около 32°C)" },
            Conds = new[] { "Небольшой дождь/Слякоть", "Ясно", "Пасмурно", "Пасмурно", "Ясно", StrangeMark }
        };

        private static readonly Season Autumn = new Season
        {
            Temps = new[] { "Прохладно (около 4°C)", "Тепло (около 15°C)", "Тепло (около 15°C)", "Тепло (около 21°C)", "Тепло (около 21°C)", "Жарко (около 27°C)" },
            Conds = new[] { "Небольшой дождь/Слякоть", "Ясно", "Пасмурно", "Пасмурно", "Ясно", StrangeMark }
        };

        // странная погода, 1d10; длительность: «d6*10» минут, «d6» часов или дней
        private static readonly string[][] StrangeTable =
        {
            new[] { "Радиоактивный ураган", "10m" },
            new[] { "Пепельная буря", "10m" },
            new[] { "Наводнение", "1d" },
            new[] { "Кровавый дождь", "1h" },
            new[] { "Кислотный дождь", "1h" },
            new[] { "Смертоносная гроза", "10m" },
            new[] { "Инверсионный смог", "1d" },
            new[] { null, "1d" },                    // Внезапное похолодание / Экстремальная жара
            new[] { "Пыльная буря", "10m" },
            new[] { "Блекаут", "1d" }
        };

        public const string ColdSnap = "Внезапное похолодание";
        public const string ExtremeHeat = "Экстремальная жара";

        public static readonly Dictionary<string, string> ConditionRules = new Dictionary<string, string>
        {
            ["Ясно"] = "Без последствий.",
            ["Пасмурно"] = "Без последствий.",
            ["Небольшой дождь/Слякоть"] = "Видимость не страдает, но поверхности скользкие: ГМ может дать −1 к подходящим проверкам (Атлетика на мокром заборе, Вождение на мокрой дороге).",
            ["Ливень"] = "Скользко и плохо видно: ГМ может дать −2 к подходящим проверкам навыков, включая Внимательность на зрение, и к дальним атакам.",
            ["Сильный дождь/Мокрый снег"] = "Скользко и плохо видно: ГМ может дать −2 к подходящим проверкам навыков, включая Внимательность на зрение, и к дальним атакам.",
            ["Радиоактивный ураган"] = "Начинается по усмотрению ГМа и длится 1d6 раундов. Кто без радиационного костюма — как под высоким уровнем радиации (CP:R стр. 181).",
            ["Пепельная буря"] = "Больше 1 минуты без назальных фильтров/респиратора — как от флакона яда (CP:R стр. 355) + критическое повреждение без бонусного урона (не лечится, пока не выйти из бури). Опционально −2 к проверкам навыков и дальним атакам.",
            ["Наводнение"] = "На улицах воды по щиколотку, в подвалах и тоннелях может затопить целиком. Глубокая вода — правила «Другие формы передвижения» (CP:R стр. 169).",
            ["Кровавый дождь"] = "Брось 1d6. 1–3: как кислотный дождь (но пахнет хуже). 4–6: раз в минуту без защиты — как доза биотоксина (CP:R стр. 355). Опционально −1 к проверкам и дальним атакам. Зонт и водостойкая подкладка дают +2 к Сопротивлению пыткам/наркотикам.",
            ["Кислотный дождь"] = "За каждую полную минуту без защиты броня изнашивается на 1 ОС. Зонт или водостойкая подкладка полностью отменяют это.",
            ["Смертоносная гроза"] = "Раз в 10 минут бросай 1d6. 1–3: молния бьёт далеко. 4–6: в самое высокое сооружение рядом; если такого нет — в самого высокого персонажа или того, кто держит двуручное металлическое оружие. Коснувшиеся поражённого (захват) получают разряд тоже. Удар: 6d6 в тело + эффект светошумовой гранаты (CP:R стр. 346). Опционально −2 к проверкам и дальним атакам.",
            ["Инверсионный смог"] = "Больше 1 минуты без назальных фильтров/противогаза — как от флакона яда (CP:R стр. 355). Опционально −4 к проверкам навыков на зрение и дальним атакам.",
            [ColdSnap] = "Заменяет бросок температуры: ниже нуля. Правила экспозиции (CP:R стр. 181). Наледь: опционально −2 к проверкам подвижности на льду.",
            [ExtremeHeat] = "Заменяет бросок температуры: 40–45°C. Правила экспозиции (CP:R стр. 181). Штраф брони к РЕА, ЛВК и СКО увеличивается на 2 (охлаждающая подкладка снижает на 1).",
            ["Пыльная буря"] = "Больше 5 минут без назальных фильтров/противогаза — критическое повреждение без бонусного урона (не лечится, пока не выйти из бури). Опционально −2 к проверкам навыков и дальним атакам.",
            ["Блекаут"] = "Нет электричества в зданиях без генератора, нет звонков и подключения к Дата Пулам. ГМ определяет район (1d10 кварталов вокруг группы)."
        };

        public static readonly Dictionary<string, string> TempRules = new Dictionary<string, string>
        {
            ["Холодно"] = "Без тёплой одежды/укрытия весь день — урон по правилам экспозиции (CP:R стр. 181). Тёплая подкладка куртки считается подходящим снаряжением.",
            ["Прохладно"] = "Комфортно, местами прохладно.",
            ["Тепло"] = "Комфортно, местами душно.",
            ["Жарко"] = "Штраф брони к РЕА, ЛВК и СКО увеличивается на 1 (охлаждающая подкладка снижает на 1)."
        };

        public static int SeasonOf(int month)
        {
            if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
            return month == 12 || month <= 2 ? 0 : month <= 5 ? 1 : month <= 8 ? 2 : 3;
        }

        public static string SeasonName(int month) => new[] { "Декабрь–Февраль", "Март–Май", "Июнь–Август", "Сентябрь–Ноябрь" }[SeasonOf(month)];

        private static Season Table(int month) => new[] { Winter, Spring, Summer, Autumn }[SeasonOf(month)];

        public static string TempClassOf(string temperature)
        {
            if (temperature == null) return null;
            foreach (var k in TempRules.Keys) if (temperature.StartsWith(k)) return k;
            return null;
        }

        // die(n) — бросок dn (1..n); для тестов можно подставить свой генератор
        public static WeatherResult Roll(int month, Func<int, int> die = null)
        {
            die = die ?? DiceRoller.Die;
            var t = Table(month);
            var r = new WeatherResult { Month = month, TempRoll = die(6), CondRoll = die(6) };
            r.Temperature = t.Temps[r.TempRoll - 1];
            r.Condition = t.Conds[r.CondRoll - 1];
            r.TempClass = TempClassOf(r.Temperature);

            if (r.Condition == StrangeMark)
            {
                r.StrangeRoll = die(10);
                var row = StrangeTable[r.StrangeRoll - 1];
                string name = row[0];
                if (name == null) name = r.TempClass == "Холодно" || r.TempClass == "Прохладно" ? ColdSnap : ExtremeHeat;
                r.Strange = name;
                r.Duration = Duration(row[1], die);
                if (name == ColdSnap) { r.Temperature = "Ниже нуля"; r.TempClass = ColdSnap; }
                else if (name == ExtremeHeat) { r.Temperature = "40–45°C"; r.TempClass = ExtremeHeat; }
            }
            r.Rules = RulesText(r);
            return r;
        }

        private static string Duration(string code, Func<int, int> die)
        {
            int d = die(6);
            switch (code)
            {
                case "10m": return d * 10 + " мин";
                case "1h": return d + (d == 1 ? " час" : d < 5 ? " часа" : " часов");
                default: return d + (d == 1 ? " день" : d < 5 ? " дня" : " дней");
            }
        }

        public static string RulesText(WeatherResult r)
        {
            var parts = new List<string>();
            string tr;
            if (r.TempClass != null && TempRules.TryGetValue(r.TempClass, out tr)) parts.Add("Температура: " + tr);
            string cr;
            string key = r.Strange ?? r.Condition;
            if (ConditionRules.TryGetValue(key, out cr)) parts.Add(key + ": " + cr);
            if (r.Strange == ColdSnap || r.Strange == ExtremeHeat) parts.RemoveAll(p => p.StartsWith("Температура:"));
            return string.Join("\n", parts);
        }

        public static string Summary(WeatherResult r)
        {
            string s = r.Temperature + ", " + (r.Strange != null ? r.Strange.ToLower() + " (" + r.Duration + ")" : r.Condition.ToLower());
            return s;
        }
    }
}
