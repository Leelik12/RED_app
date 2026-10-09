using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    // Шесть ночных рынков из «Чёрного хрома» (гл. «Ночные рынки») и генератор ассортимента на вечер.
    // Описания рынков пересказаны по книге; сам ассортимент собирается случайно из каталогов по специализации
    // рынка — это инструмент ГМа, в книге списков товаров по вечерам нет.
    public class NightMarketDef
    {
        public string Id, Name, NameEn, Runner, Frequency, Place, Invite, Sells;
        public Func<PackItem, bool> Filter;
        public int MinItems, MaxItems;
        public string StockNote;     // как читать ассортимент
    }

    public static class NightMarket
    {
        public static readonly List<NightMarketDef> All = new List<NightMarketDef>
        {
            new NightMarketDef
            {
                Id = "threepiece", Name = "Объединение 3-Пийс", NameEn = "3-Piece's Joint", Runner = "3-Пийс (Фиксер), его муж Брейн и дочь Баг",
                Frequency = "Раз в месяц, в случайные дни — когда накопится товар.",
                Place = "Верхний этаж бара Short Circuit в Малой Европе, на краю Горячей зоны.",
                Invite = "Только для давних посетителей бара; решающее слово за Баг — она чует крыс, корпоратов и предателей.",
                Sells = "Техника: детали, кибердеки, железо для деков и программы. Ничего сломанного и недоделанного, часто лучше нового.",
                StockNote = "Всё исправно; техника, деки и железо.",
                MinItems = 6, MaxItems = 14,
                Filter = i => i.Kind == "deck" || i.Kind == "netware" || InGroup(i, "Кибердеки", "Оборудование кибердеки", "Снаряжение для взлома", "Техника", "Дроны", "Оптика")
            },
            new NightMarketDef
            {
                Id = "woodchipper", Name = "Контейнерная Вечеринка Вудчипер", NameEn = "Woodchipper's Block Party", Runner = "Вудчипер (Фиксер, посредница Альдекальдо)",
                Frequency = "Примерно раз в два месяца; зависит от кочевых Наций и погоды (кровавый дождь всё портит).",
                Place = "Тупик в Санто-Доминго рядом с гаражом Вудчипер, на улице, под гирляндами и живую музыку.",
                Invite = "Кочевник в хорошем положении у Альдекальдо, Джодов или Телас + до четырёх гостей; остальным — заработать приглашение.",
                Sells = "Оружие для всех приглашённых, транспорт — только проверенным Фиксерам; снаряжение для жизни на природе, боеприпасы.",
                StockNote = "Транспорт отпускается только проверенным Фиксерам.",
                MinItems = 8, MaxItems = 16,
                Filter = i => i.Kind == "weapon" || i.Kind == "ammo" || i.Kind == "vehicle" || InGroup(i, "Выживание", "Погодная защита", "Взрывчатка", "Гранаты")
            },
            new NightMarketDef
            {
                Id = "molly", Name = "Рынок Молли Андерсон", NameEn = "Molly Anderson's Market", Runner = "Молли Андерсон (глава Семейной банды Андерсонов)",
                Frequency = "Раз в месяц; чем опаснее в Старой боевой зоне, тем больше хлама на продажу.",
                Place = "Подземная парковка под рухнувшим небоскрёбом в Старой боевой зоне — один вход и выход, оборудована как крепость.",
                Invite = "Заслужить: приглашения проходят через руки Андерсонов; репутация в Старой боевой зоне и Старом Японтауне помогает, часто просят отработать смену охраны.",
                Sells = "Всего понемногу, но почти всё — б/у: особенно киберимпланты (с прежним владельцем), подержанное оружие и броня, перекрашенные и подновлённые.",
                StockNote = "Всё бывшее в употреблении; цена по каталогу — торгуйтесь по обстановке.",
                MinItems = 8, MaxItems = 16,
                Filter = i => i.Kind == "cyber" || i.Kind == "weapon" || i.Kind == "armor"
            },
            new NightMarketDef
            {
                Id = "kernaghan", Name = "Полуночный Рынок Мистера Кей", NameEn = "Mister K's Midnight Market", Runner = "Мистер Кернаган (редко появляется до аукциона)",
                Frequency = "Раз в квартал, без фиксированных дат — когда у Кернагана набирается достаточно интересных вещей.",
                Place = "Два этажа над клубом «Атлантис» в Глене; роскошные бутики, рестораны и услуги.",
                Invite = "Сто именных приглашений доставляются курьерами; приглашение можно продать, подарить или украсть, с ним можно привести свиту.",
                Sells = "Товары для состоятельных клиентов и коллекция сверхроскошных лотов от 10 000 eb: автомобили, пропавшие произведения искусства, уникальные прототипы.",
                StockNote = "Лоты аукциона — только вещи дороже 10 000 eb.",
                MinItems = 3, MaxItems = 6,
                Filter = i => i.Cost >= 10000
            },
            new NightMarketDef
            {
                Id = "mynah", Name = "Ночной Рынок Мисс Майна", NameEn = "Ms. Mynah's Night Market", Runner = "Мисс Майна (Фиксер)",
                Frequency = "Один день в месяц, всегда пятого числа.",
                Place = "Огромный бывший торговый центр Minimallism в Ранчо-Коронадо; делится с Реклеймерами, которые выращивают в нём еду (комплекс зовут «РК Ночной Рынок»).",
                Invite = "По пригласительному коду, который раз в месяц получают пятьсот клиентов через приложение Zchat.",
                Sells = "Одежда и мода: автоматическая снятка мерок, индивидуальный пошив и готовые образы из контейнеров и переработанной ткани; еда, напитки и услуги.",
                StockNote = "Готовая одежда и броня-мода; индивидуальный пошив обсуждается отдельно.",
                MinItems = 6, MaxItems = 14,
                Filter = i => i.Kind == "armor" || InGroup(i, "Одежда", "Образ жизни", "Развлечения", "Бар", "Погодная защита")
            },
            new NightMarketDef
            {
                Id = "rex", Name = "Казино Рекса Рояля", NameEn = "Rex Royale's Casino", Runner = "Рекс Рояль",
                Frequency = "Примерно раз в месяц — как только хватает времени и товара, а при крупной партии и раньше.",
                Place = "Каждый раз новое место: заброшенное здание с крепким подвалом севернее границы Малого Китая и Верхней Марины.",
                Invite = "Именные игральные карты из особой колоды — всего 52 приглашения на рынок; можно привести одного-двух гостей, «джокеров не берём».",
                Sells = "То же, что любит сам Рекс: бронежилеты и оружие — чем ярче, тем лучше; иногда и другое. По слухам, скидка за победу в азартной игре, но платить придётся услугами.",
                StockNote = "Броня и оружие с претензией на показушность.",
                MinItems = 8, MaxItems = 14,
                Filter = i => i.Kind == "armor" || i.Kind == "weapon"
            },
        };

        public static NightMarketDef Find(string id) => All.FirstOrDefault(m => m.Id == id);

        private static bool InGroup(PackItem i, params string[] groups)
            => i.Kind == "gear" && groups.Contains(i.Group);

        private static bool ContainsAny(PackItem i, params string[] parts)
        {
            string hay = ((i.Name ?? "") + " " + (i.Group ?? "")).ToLowerInvariant();
            return parts.Any(p => hay.Contains(p));
        }

        public static List<PackItem> Pool(NightMarketDef m)
            => Packs.Items.Where(i => Packs.IsEnabled(i.Pack) && m.Filter(i)).ToList();

        // случайный ассортимент на вечер: без повторов, отсортирован по виду и цене
        public static List<PackItem> Generate(NightMarketDef m, Random rng)
        {
            var pool = Pool(m);
            int n = m.MinItems + rng.Next(m.MaxItems - m.MinItems + 1);
            n = Math.Min(n, pool.Count);
            var picked = new List<PackItem>();
            for (int k = 0; k < n; k++)
            {
                int idx = rng.Next(pool.Count);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return picked.OrderBy(i => i.Kind).ThenBy(i => i.Cost).ToList();
        }

        public static string KindName(string kind)
        {
            switch (kind)
            {
                case "weapon": return "Оружие";
                case "armor": return "Броня";
                case "cyber": return "Импланты";
                case "vehicle": return "Транспорт";
                case "ammo": return "Боеприпасы";
                case "drug": return "Препараты";
                case "deck": return "Кибердеки";
                case "netware": return "Железо и программы";
                case "property": return "Недвижимость";
                default: return "Снаряжение";
            }
        }
    }
}
