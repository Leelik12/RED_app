using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    // штаб-квартира команды (DLC «Нет места лучше дома»): ОУ ШК, 12 улучшений и их модернизации
    public class HqData
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public int Rent { get; set; }                                   // общая аренда в месяц
        public int Earned { get; set; }                                 // накопленные ОУ ШК
        public int BaseBeds { get; set; } = 1;                          // спальных мест до «Снижения аренды»
        public List<string> Upgrades { get; set; } = new List<string>();     // uid купленных улучшений
        public List<string> Modded { get; set; } = new List<string>();       // uid модернизированных (кроме счётных)
        public int RentMods { get; set; }
        public int MoralMods { get; set; }
        public string Notes { get; set; }
        public List<string> Log { get; set; } = new List<string>();
    }

    public static class Hq
    {
        public const int Cost = 40;
        public const string UpgradeGroup = "Улучшения штаб-квартиры", MoralGroup = "Моральный Буст: модернизации";

        public static List<PackItem> Upgrades() => Packs.Enabled("other").Where(i => i.Group == UpgradeGroup).ToList();
        public static List<PackItem> MoralTable() => Packs.Enabled("other").Where(i => i.Group == MoralGroup).OrderBy(i => int.Parse(i.Get("n"))).ToList();
        public static PackItem FactionRule() => Packs.Enabled("other").FirstOrDefault(i => i.Get("uid") == "faction");

        public static bool Has(HqData h, string uid) => h.Upgrades.Contains(uid);

        // потрачено: 40 за каждое купленное улучшение и каждую модернизацию
        public static int Spent(HqData h) => Cost * (h.Upgrades.Count + h.Modded.Count + h.RentMods + h.MoralMods);
        public static int Remaining(HqData h) => h.Earned - Spent(h);

        public static int MaxRentMods(HqData h) => Math.Max(1, h.BaseBeds);
        public static int MaxMoralMods => 10;

        public static bool CanBuy(HqData h, string uid) => !Has(h, uid) && Remaining(h) >= Cost;

        public static bool CanMod(HqData h, string uid, bool multi)
        {
            if (!Has(h, uid) || Remaining(h) < Cost) return false;
            if (uid == "rent") return h.RentMods < MaxRentMods(h);
            if (uid == "moral") return h.MoralMods < MaxMoralMods;
            return !h.Modded.Contains(uid);
        }

        public static bool Buy(HqData h, string uid)
        {
            if (!CanBuy(h, uid)) return false;
            h.Upgrades.Add(uid);
            return true;
        }

        public static bool Mod(HqData h, string uid)
        {
            if (!CanMod(h, uid, uid == "rent" || uid == "moral")) return false;
            if (uid == "rent") h.RentMods++;
            else if (uid == "moral") h.MoralMods++;
            else h.Modded.Add(uid);
            return true;
        }

        // снять модернизацию (ОУ возвращаются — решение стола)
        public static void Unmod(HqData h, string uid)
        {
            if (uid == "rent") h.RentMods = Math.Max(0, h.RentMods - 1);
            else if (uid == "moral") h.MoralMods = Math.Max(0, h.MoralMods - 1);
            else h.Modded.Remove(uid);
        }

        // убрать улучшение вместе с его модернизациями
        public static void Remove(HqData h, string uid)
        {
            h.Upgrades.Remove(uid);
            h.Modded.Remove(uid);
            if (uid == "rent") h.RentMods = 0;
            if (uid == "moral") h.MoralMods = 0;
        }

        public static void AddPoints(HqData h, int points, string note)
        {
            if (points == 0) return;
            h.Earned = Math.Max(0, h.Earned + points);
            h.Log.Insert(0, (points > 0 ? "+" : "") + points + " ОУ ШК" + (string.IsNullOrWhiteSpace(note) ? "" : " — " + note));
        }

        // номера выгод Морального Буста, действующие при n модернизациях (4 заменяет 1, 8 — 6, 9 — 4)
        public static List<int> ActiveMoral(int n)
        {
            var list = Enumerable.Range(1, Math.Min(10, Math.Max(0, n))).ToList();
            if (n >= 4) list.Remove(1);
            if (n >= 8) list.Remove(6);
            if (n >= 9) list.Remove(4);
            return list;
        }

        // ежемесячная экономия на образе жизни каждого члена команды
        public static int LifestyleDiscount(HqData h) => Has(h, "moral") ? 50 : 0;

        // сколько прибавить к ТЕЛ при расчёте естественного исцеления
        public static int HealBodyBonus(HqData h) => (Has(h, "medbay") ? 2 : 0) + (Has(h, "moral") && h.MoralMods >= 2 ? 1 : 0);

        // бонус к УДЧ команды от 3-й и 7-й модернизаций Морального Буста
        public static int LuckBonus(HqData h) => !Has(h, "moral") ? 0 : (h.MoralMods >= 3 ? 1 : 0) + (h.MoralMods >= 7 ? 1 : 0);

        public static int Beds(HqData h) => h.BaseBeds + (Has(h, "rent") ? h.RentMods : 0);

        public static string RentLine(HqData h)
            => h.Rent + "eb в месяц" + (Has(h, "rent") ? " (после «Снижения аренды» считайте по категории ниже)" : "");

        // краткая сводка действующих бонусов
        public static List<string> Benefits(HqData h)
        {
            var list = new List<string>();
            foreach (var u in Upgrades())
            {
                string uid = u.Get("uid");
                if (!Has(h, uid)) continue;
                bool modded = uid == "rent" ? h.RentMods > 0 : uid == "moral" ? h.MoralMods > 0 : h.Modded.Contains(uid);
                list.Add(u.Name + (modded ? " (модерн.)" : ""));
            }
            if (LifestyleDiscount(h) > 0) list.Add("Образ жизни: −" + LifestyleDiscount(h) + "eb в месяц на каждого");
            if (HealBodyBonus(h) > 0) list.Add("Исцеление: ТЕЛ +" + HealBodyBonus(h));
            if (LuckBonus(h) > 0) list.Add("УДЧ команды: +" + LuckBonus(h));
            if (Has(h, "rent")) list.Add("Спальных мест: " + Beds(h));
            return list;
        }

        // пример из книги: ШК Cyber6 после третьего акта
        public static HqData SampleCyber6()
        {
            var h = new HqData
            {
                Name = "ШК Cyber6", Location = "Санто-Доминго, гаражи Вудчиппер", Rent = 3000, BaseBeds = 1, Earned = 400,
                Description = "Три отдельно стоящих гаражных блока, каждый с прилегающей комнатой и ванной, эквивалентные трём квартирам-студиям. Сзади небольшой сад.",
            };
            foreach (var u in new[] { "clues", "garage", "moral", "rent", "server", "studio", "training", "workshop" }) h.Upgrades.Add(u);
            h.Modded.Add("garage"); h.Modded.Add("server");
            return h;
        }
    }
}
