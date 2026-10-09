using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberpunkRED_Generator
{
    public class FinanceEntry
    {
        public string Stamp { get; set; }
        public string Text { get; set; }
        public int Amount { get; set; }
        public int Balance { get; set; }
    }

    // деньги, образ жизни и жильё персонажа (хранится в файле сохранения)
    public class FinanceData
    {
        public string LifestyleName { get; set; }
        public string HousingName { get; set; }
        public bool HousingOwned { get; set; }
        public int MonthsPaid { get; set; }
        public List<FinanceEntry> Log { get; set; } = new List<FinanceEntry>();
    }

    public class LifestyleDef { public string Name; public int Cost; public string Info; public string Details; }
    public class HousingDef
    {
        public string Name;
        public int Rent;           // 0 — аренды нет (улица, выдаётся корпорацией)
        public int Buy;            // 0 — купить нельзя
        public bool Provided;      // даётся корпорацией
        public string Info;
    }
    public class ServiceDef { public string Name; public int Cost; }

    // образ жизни, жильё и услуги из книги (глава «Новая экономика улиц», стр. 376–380)
    public static class FinanceCatalog
    {
        public static readonly List<LifestyleDef> Lifestyles = new List<LifestyleDef>
        {
            new LifestyleDef { Name = "На киббле", Cost = 100, Info = "Отвратительная еда. Раз в месяц кино или брейнданс.",
                Details = "Еда: 2 порции киббла в день. Связь: учётка в агенте (дата-пул, звонки, сообщения). Развлечения: раз в месяц на 20eb (Обычное). Транспорт: проездной на автобус и метро; раз в месяц такси в район и из района с угрозой Умеренной, Корпоративной или Административной." },
            new LifestyleDef { Name = "На обычных полуфабрикатах", Cost = 300, Info = "Еда вкуснее киббла. Каждые выходные хороший бар или приличный ресторан.",
                Details = "Еда: порция киббла и 2 обычных полуфабриката в день. Связь: как выше + сообщение в другой город раз в неделю. Развлечения: раз в неделю на 20eb. Транспорт: проездной; такси раз в неделю (те же районы)." },
            new LifestyleDef { Name = "На хороших полуфабрикатах", Cost = 600, Info = "Еда как в большинстве ресторанов. Отличные бары и рестораны, раз в месяц концерт или спортивное событие.",
                Details = "Еда: обычный полуфабрикат и 2 хороших в день. Связь: сообщение в другой город раз в день, 10-минутный голосовой звонок раз в неделю. Развлечения: раз в день на 20eb, раз в месяц на 100eb (Премиум). Транспорт: проездной; такси раз в день; раз в месяц компактный автомобиль на 24 часа (корник, с. 190)." },
            new LifestyleDef { Name = "На свежей еде", Cost = 1500, Info = "Настоящая еда. Раз в месяц отель без оглядки на бюджет, бар для руководства, ресторан мирового класса.",
                Details = "Еда: хороший полуфабрикат и 2 блюда свежей еды в день. Связь: сообщения без ограничений, 10-минутный голосовой звонок раз в день, видеозвонок раз в неделю. Развлечения: раз в день на 50eb (Ценное), раз в неделю на 100eb, раз в месяц на 500eb (Дорогое). Транспорт: проездной; такси раз в день в любой район; раз в неделю компактный автомобиль на 24 часа. Разбили арендованную машину — следующую в этом месяце могут не дать." },
        };

        // «Экономика 101» («Чёрный хром», с. 130): СЛ Знатока улиц, чтобы найти Фиксера или Техника под вещь; null — вещь дешевле 500eb, проверка не нужна
        public static int? StreetwiseDv(int cost)
        {
            if (cost >= 10000) return 24;
            if (cost >= 5000) return 21;
            if (cost >= 1000) return 17;
            if (cost >= 500) return 15;
            return null;
        }

        public static readonly List<HousingDef> Housing = new List<HousingDef>
        {
            new HousingDef { Name = "Жизнь на улице", Info = "Каждую ночь проверка Выносливости СЛ15, иначе измотан (−2 ко всему)." },
            new HousingDef { Name = "Жизнь на улице в транспорте", Info = "Нужен застеклённый транспорт. СЛ15 Выносливости без кровати." },
            new HousingDef { Name = "Куб-отель", Rent = 500, Info = "Одна комната без окон. Двое в кубе — всегда измотаны." },
            new HousingDef { Name = "Грузовой контейнер", Rent = 1000, Buy = 15000, Info = "Кровать, стол, электричество, холодильник; удобства общие." },
            new HousingDef { Name = "Квартира-студия", Rent = 1500, Buy = 25000, Info = "Своя ванная и мини-кухня, 1 защищённое парковочное место." },
            new HousingDef { Name = "Двухкомнатная квартира", Rent = 2500, Buy = 35000, Info = "Две спальни, полноценная кухня, 2 места парковки." },
            new HousingDef { Name = "Корпоративный конапт", Provided = true, Info = "Выдаётся корпорацией; слежка во всех комнатах кроме ванных." },
            new HousingDef { Name = "Улучшенный конапт", Rent = 7500, Buy = 85000, Info = "Двухэтажный, 3 места парковки (одно на крыше)." },
            new HousingDef { Name = "Роскошный пентхаус", Rent = 15000, Buy = 150000, Info = "Вертолётная площадка, 4 места парковки, горничная." },
            new HousingDef { Name = "Корпоративный дом в Бивервилле", Provided = true, Buy = 200000, Info = "Только покупка или выдача корпорацией." },
            new HousingDef { Name = "Корпоративный особняк в Бивервилле", Provided = true, Buy = 500000, Info = "Только покупка или выдача корпорацией." },
        };

        public static readonly List<ServiceDef> Services = new List<ServiceDef>
        {
            new ServiceDef { Name = "Бодискульптинг (стандартный)", Cost = 500 },
            new ServiceDef { Name = "Бодискульптинг (экзотический)", Cost = 1000 },
            new ServiceDef { Name = "Брейнданс", Cost = 20 },
            new ServiceDef { Name = "Напиток, дайв-бар", Cost = 10 },
            new ServiceDef { Name = "Напиток, хороший бар", Cost = 10 },
            new ServiceDef { Name = "Напиток, отличный бар", Cost = 20 },
            new ServiceDef { Name = "Напиток, представительский бар", Cost = 50 },
            new ServiceDef { Name = "Установка найденных киберимплантов (ТЦ)", Cost = 100 },
            new ServiceDef { Name = "Установка найденных киберимплантов (клиника)", Cost = 500 },
            new ServiceDef { Name = "Установка найденных киберимплантов (больница)", Cost = 1000 },
            new ServiceDef { Name = "Лечение в больнице (СЛ 10)", Cost = 50 },
            new ServiceDef { Name = "Лечение в больнице (СЛ 13)", Cost = 100 },
            new ServiceDef { Name = "Лечение в больнице (СЛ 15)", Cost = 500 },
            new ServiceDef { Name = "Лечение в больнице (СЛ 17+)", Cost = 1000 },
            new ServiceDef { Name = "Отель, за ночь", Cost = 100 },
            new ServiceDef { Name = "Роскошный отель, за ночь", Cost = 500 },
            new ServiceDef { Name = "Интерактивный брейнданс", Cost = 50 },
            new ServiceDef { Name = "Живой концерт / спортивное событие", Cost = 100 },
            new ServiceDef { Name = "Фильм", Cost = 20 },
            new ServiceDef { Name = "Профессиональные услуги, хорошие, за час", Cost = 100 },
            new ServiceDef { Name = "Профессиональные услуги, отличные, за час", Cost = 500 },
            new ServiceDef { Name = "Профессиональные услуги, мировой класс, за заказ", Cost = 5000 },
            new ServiceDef { Name = "Ресторан, фастфуд", Cost = 10 },
            new ServiceDef { Name = "Ресторан, хороший", Cost = 20 },
            new ServiceDef { Name = "Ресторан, отличный", Cost = 50 },
            new ServiceDef { Name = "Ресторан, мировой класс", Cost = 500 },
            new ServiceDef { Name = "Такси", Cost = 20 },
            new ServiceDef { Name = "Терапия (стандартная потеря человечности)", Cost = 500 },
            new ServiceDef { Name = "Терапия (экстремальная потеря человечности)", Cost = 1000 },
            new ServiceDef { Name = "Терапия (зависимость)", Cost = 1000 },
            new ServiceDef { Name = "Подписка Trauma Team Silver, в месяц", Cost = 500 },
            new ServiceDef { Name = "Подписка Trauma Team Executive, в месяц", Cost = 1000 },
            new ServiceDef { Name = "Видеоигра", Cost = 50 },
        };

        // месячный счёт: образ жизни + аренда (если жильё не куплено и не выдано корпорацией)
        public static int MonthlyBill(LifestyleDef lifestyle, HousingDef housing, bool owned)
        {
            int total = lifestyle?.Cost ?? 0;
            if (housing != null && !owned && !housing.Provided) total += housing.Rent;
            return total;
        }

        // «1 500», «1500eb», «1,500» -> 1500; не число — false
        public static bool TryParseMoney(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var m = Regex.Match(text, @"-?\d[\d\s\u00A0,.]*");
            if (!m.Success) return false;
            string digits = Regex.Replace(m.Value, @"[\s\u00A0,.\-]", "");
            bool neg = m.Value.StartsWith("-");
            if (!int.TryParse(digits, out int v)) return false;
            value = neg ? -v : v;
            return true;
        }

        public static string FormatMoney(int value)
        {
            string s = System.Math.Abs(value).ToString();
            for (int i = s.Length - 3; i > 0; i -= 3) s = s.Insert(i, " ");
            return (value < 0 ? "−" : "") + s;
        }
    }
}
