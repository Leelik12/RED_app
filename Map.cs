using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace CyberpunkRED_Generator
{
    public class MapPlace
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
    }

    // своя метка ГМа на карте; хранится в settings.json
    public class MapPin
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        public double X { get; set; }
        public double Y { get; set; }
        public string Title { get; set; } = "Метка";
        public string Note { get; set; } = "";
    }

    public class MapDistrict
    {
        public string Letter { get; set; }
        public string Name { get; set; }
        public string Side { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int MX { get; set; }             // положение на растровой карте (если она встроена)
        public int MY { get; set; }
        public string Kind { get; set; }        // executive | rebuilding | suburbs | combat | hot | perimeter
        public bool Approx { get; set; }
        public string Manager { get; set; }
        public string Security { get; set; }
        public string Gangs { get; set; }
        public List<MapPlace> Places { get; set; } = new List<MapPlace>();
    }

    // Карта Найт-Сити: 24 района из справочной главы корника (Data/map.json) и метки поверх неё —
    // недвижимость из DLC и ночные рынки. Положение районов схематично.
    public static class CityMap
    {
        public const string ResourceName = "Data.map.json";
        private static List<MapDistrict> _all;
        private static readonly object Lock = new object();

        public static IReadOnlyList<MapDistrict> Districts { get { Ensure(); return _all; } }

        private class Doc { public int Version { get; set; } public List<MapDistrict> Districts { get; set; } }

        private static void Ensure()
        {
            if (_all != null) return;
            lock (Lock)
            {
                if (_all != null) return;
                var list = new List<MapDistrict>();
                try
                {
                    using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                        if (s != null) list = Parse(s);
                }
                catch (Exception ex) { ErrorLog.Write("map", ex); }
                _all = list;
            }
        }

        public static List<MapDistrict> Parse(Stream s)
            => JsonSerializer.Deserialize<Doc>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Districts ?? new List<MapDistrict>();

        public static MapDistrict Get(string letter) => Districts.FirstOrDefault(d => d.Letter == letter);

        public static string KindName(string kind)
        {
            switch (kind)
            {
                case "executive": return "Представительская зона";
                case "rebuilding": return "Восстанавливающийся центр";
                case "suburbs": return "Переполненные пригороды";
                case "combat": return "Боевая зона";
                case "hot": return "Горячая зона";
                case "perimeter": return "Отвоёванный периметр";
                default: return "";
            }
        }

        // ночные рынки по районам книги
        public static readonly Dictionary<string, string> MarketDistrict = new Dictionary<string, string>
        {
            { "threepiece", "A" }, { "woodchipper", "V" }, { "molly", "L" }, { "kernaghan", "G" }, { "mynah", "X" }, { "rex", "E" }
        };

        // как называют район в описаниях недвижимости DLC (поле «zone»); порядок важен — длинные названия раньше коротких
        private static readonly (string Key, string Letter)[] ZoneNames =
        {
            ("Курорт Пасифика", "W"), ("Пасифика", "W"), ("Маленькая Европа", "A"), ("Малая Европа", "A"), ("Маленький Китай", "E"),
            ("Уотсон", "N"), ("Аппер Марина", "B"), ("Верхняя Гавань", "B"), ("Новый Вестбрук", "P"), ("Нью-Уэстбрук", "P"),
            ("Южный Найт-Сити", "I"), ("Университетский район", "F"), ("Глен", "G"), ("Старый Джапантаун", "H"),
            ("Старая боевая зона", "L"), ("Военная база", "M"), ("Ранчо Коронадо", "X"), ("Санто-Доминго", "V"),
            ("Индустриальная зона Хейвуда", "U"), ("Промзона Хейвуда", "U"), ("Окраина Хейвуда", "U"), ("Хейвуд", "U"),
            ("Зоны Поражения", "D"), ("Зона Поражения", "D"), ("Кабуки", "O"), ("Чартер-Хилл", "Q"), ("Порт", "J"), ("Даунтаун", "C")
        };

        public const string MorroRock = "Морро-Рок";

        // районы, на которые указывает строка «zone» (может перечислять несколько через «/»); Морро-Рок — отдельная метка вне районов
        public static List<string> ZoneLetters(string zone)
        {
            var res = new List<string>();
            if (string.IsNullOrWhiteSpace(zone)) return res;
            foreach (var part in zone.Split('/'))
            {
                var p = part.Trim();
                if (p.StartsWith(MorroRock, StringComparison.OrdinalIgnoreCase)) { res.Add(MorroRock); continue; }
                foreach (var z in ZoneNames)
                    if (p.IndexOf(z.Key, StringComparison.OrdinalIgnoreCase) >= 0) { if (!res.Contains(z.Letter)) res.Add(z.Letter); break; }
            }
            return res;
        }

        // слои карты, которые можно скрыть; ключи недвижимости совпадают с Extra.type предметов DLC
        public static readonly (string Id, string Name)[] Layers =
        {
            ("cube", "🛏 Куботели"), ("container", "📦 Контейнеры"), ("conapt", "🏢 Конапты"), ("studio", "🏠 Студии"), ("market", "🌙 Ночные рынки"), ("custom", "📍 Мои метки")
        };

        public static bool LayerOn(string id) => !AppSettings.Data.MapHidden.Contains(id);

        public static void SetLayer(string id, bool on)
        {
            var h = AppSettings.Data.MapHidden;
            if (on) h.Remove(id); else if (!h.Contains(id)) h.Add(id);
            AppSettings.Save();
        }

        // недвижимость включённых пакетов, привязанная к району (или к Морро-Року)
        public static List<PackItem> Properties(string letter)
            => Packs.Items.Where(i => i.Kind == "property" && Packs.IsEnabled(i.Pack) && ZoneLetters(i.Get("zone")).Contains(letter)).OrderBy(i => i.Name).ToList();

        // то же, но только из видимых слоёв
        public static List<PackItem> VisibleProperties(string letter)
            => Properties(letter).Where(i => LayerOn(i.Get("type") ?? "")).ToList();

        public static List<NightMarketDef> VisibleMarkets(string letter) => LayerOn("market") ? Markets(letter) : new List<NightMarketDef>();

        public static MapPin AddPin(double x, double y)
        {
            var pin = new MapPin { X = Math.Round(x), Y = Math.Round(y) };
            AppSettings.Data.MapPins.Add(pin);
            AppSettings.Save();
            return pin;
        }

        public static void RemovePin(string id)
        {
            AppSettings.Data.MapPins.RemoveAll(p => p.Id == id);
            AppSettings.Save();
        }

        public static List<NightMarketDef> Markets(string letter)
            => NightMarket.All.Where(m => MarketDistrict.TryGetValue(m.Id, out var l) && l == letter).ToList();

        // районы, в которых что-то нашлось по запросу: название, менеджер, банды, места
        public static bool Matches(MapDistrict d, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            string q = query.Trim();
            bool Has(string s) => s != null && s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
            return Has(d.Name) || Has(d.Manager) || Has(d.Security) || Has(d.Gangs) || d.Places.Any(p => Has(p.Name) || Has(p.Text));
        }
    }
}
